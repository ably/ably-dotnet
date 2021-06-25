using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Ably.Push
{
    internal partial class ActivationStateMachine
    {
        private SemaphoreSlim _handleEventsLock = new SemaphoreSlim(1, 1);
        private Queue<Event> _pendingEvents = new Queue<Event>();

        private readonly AblyRest _restClient;
        private readonly IMobileDevice _mobileDevice;
        private readonly ILogger _logger;

        public string ClientId { get; }

        public State CurrentState { get; private set; }

        private Queue<Event> PendingEvents { get; set; } = new Queue<Event>();

        public LocalDevice LocalDevice { get; set; } = new LocalDevice();

        private void Debug(string message) => _logger.Debug($"ActivationStateMachine: {message}");

        private void Error(string message, Exception ex) => _logger.Error($"ActivationStateMachine: {message}", ex);

        internal ActivationStateMachine(AblyRest restClient, IMobileDevice mobileDevice, ILogger logger)
        {
            _restClient = restClient;
            ClientId = _restClient.Auth.ClientId;
            _mobileDevice = mobileDevice;
            _logger = logger;
            CurrentState = new NotActivated(this);
        }

        public static ActivationStateMachine CreateAndLoadState(AblyRest restClient, IMobileDevice mobileDevice, ILogger logger)
        {
            var stateMachine = new ActivationStateMachine(restClient, mobileDevice, logger);
            stateMachine.LoadPersistedState();
            stateMachine.EnsureLocalDeviceIsLoaded();

            return stateMachine;
        }

        private void CallDeactivatedCallback(ErrorInfo reason)
        {
            SendErrorIntent("PUSH_DEACTIVATE", reason); // TODO: Put intent names in consts
        }

        private async Task ValidateRegistration()
        {
            Debug("Validating Registration");

            // Make sure the call is not completed synchronously
            await Task.Yield();

            // TODO: See if I need to get Ably from some kind of context
            var presentClientId = _restClient.Auth.ClientId;
            if (presentClientId.IsNotEmpty() && presentClientId.EqualsTo(LocalDevice.ClientId) == false)
            {
                Debug(
                    $"Activation failed. Auth clientId '{presentClientId}' is not equal to Device clientId '{LocalDevice.ClientId}'");

                var error = new ErrorInfo(
                    "Activation failed: present clientId is not compatible with existing device registration",
                    ErrorCodes.ActivationFailedClientIdMismatch,
                    HttpStatusCode.BadRequest);

                _ = HandleEvent(new SyncRegistrationFailed(error));
                return;
            }

            try
            {
                await _restClient.Push.Admin.DeviceRegistrations.SaveAsync(LocalDevice);

                // TODO: SetClientId from returned devices in case it has been set differently
                _ = HandleEvent(new RegistrationSynced());
            }
            catch (AblyException e)
            {
                // TODO: Log
                _ = HandleEvent(new SyncRegistrationFailed(e.ErrorInfo));
            }
        }

        public async Task HandleEvent(Event @event)
        {
            Debug($"Handling event ({@event.GetType().Name}.");

            await Task.Yield();

            var canEnter = await _handleEventsLock.WaitAsync(1000); // Arbitrary number = 1 second
            if (canEnter)
            {
                try
                {
                    // Log.d(TAG, String.format("handling event %s from %s", event.getClass().getSimpleName(), current.getClass().getSimpleName()));
                    State maybeNext = await CurrentState.Transition(@event);
                    if (maybeNext == null)
                    {
                        _pendingEvents.Enqueue(@event);
                        PersistState();
                        return;
                    }

                    CurrentState = maybeNext;

                    while (true)
                    {
                        var pending = _pendingEvents.Peek();
                        if (pending == null)
                        {
                            break;
                        }

                        // TODO: Log
                        var nextState = await CurrentState.Transition(pending);
                        if (nextState == null)
                        {
                            break;
                        }

                        _ = _pendingEvents.Dequeue(); // Remove the message from the queue
                        CurrentState = nextState;
                    }

                    PersistState();
                }
                finally
                {
                    _handleEventsLock.Release();
                }
            }
            else
            {
                _pendingEvents.Enqueue(@event);
                return;
            }
        }

        private void PersistState()
        {
            if (CurrentState != null && CurrentState.Persist)
            {
                _mobileDevice.SetPreference(PersistKeys.StateMachine.CURRENT_STATE, CurrentState.GetType().Name, PersistKeys.StateMachine.SharedName);
            }

            var events = _pendingEvents.ToList();

            _mobileDevice.SetPreference(PersistKeys.StateMachine.PENDING_EVENTS_LENGTH, events.Count.ToString(), PersistKeys.StateMachine.SharedName);

            // Saves pending events as a pipe separated list.
            _mobileDevice.SetPreference(PersistKeys.StateMachine.PENDING_EVENTS, events.Select(x => x.GetType().Name).JoinStrings("|"), PersistKeys.StateMachine.SharedName);
        }

        private void AddToEventQueue(Event @event)
        {
            PendingEvents.Enqueue(@event);
        }

        private void GetRegistrationToken()
        {
            _mobileDevice.RequestRegistrationToken(result =>
            {
                if (result.IsSuccess)
                {
                    var token = result.Value;
                    var previous = LocalDevice.RegistrationToken;
                    if (previous != null)
                    {
                        if (previous.Token.EqualsTo(token))
                        {
                            return;
                        }
                    }

                    // TODO: Log
                    var registrationToken = new RegistrationToken(RegistrationToken.Fcm, token);
                    LocalDevice.RegistrationToken = registrationToken;

                    // TODO: What happens if this errors
                    PersistRegistrationToken(registrationToken);

                    _ = HandleEvent(new GotPushDeviceDetails());
                }
                else
                {
                    // Todo: Log
                    _ = HandleEvent(new GettingPushDeviceDetailsFailed(result.Error));
                }
            });
        }

        private void PersistRegistrationToken(RegistrationToken token)
        {
            _mobileDevice.SetPreference(PersistKeys.Device.TOKEN_TYPE, token.Type, PersistKeys.Device.SharedName);
            _mobileDevice.SetPreference(PersistKeys.Device.TOKEN, token.Token, PersistKeys.Device.SharedName);
        }

        private void PersistLocalDevice(LocalDevice localDevice)
        {
            _mobileDevice.SetPreference(PersistKeys.Device.DEVICE_ID, localDevice.Id, PersistKeys.Device.SharedName);
            _mobileDevice.SetPreference(PersistKeys.Device.CLIENT_ID, localDevice.ClientId, PersistKeys.Device.SharedName);
            _mobileDevice.SetPreference(PersistKeys.Device.DEVICE_SECRET, localDevice.DeviceSecret, PersistKeys.Device.SharedName);
        }

        private void SendErrorIntent(string name, ErrorInfo errorInfo)
        {
            var properties = new Dictionary<string, object>();
            properties.Add("hasError", errorInfo != null);
            if (errorInfo != null)
            {
                properties.Add("error.message", errorInfo.Message);
                properties.Add("error.statusCode", (int?)errorInfo.StatusCode);
                properties.Add("error.code", errorInfo.Code);
            }

            _mobileDevice.SendIntent(name, properties);
        }

        private void SendIntent(string name)
        {
            _mobileDevice.SendIntent(name, new Dictionary<string, object>());
        }

        private void SetDeviceIdentityToken(string deviceIdentityToken)
        {
            LocalDevice.DeviceIdentityToken = deviceIdentityToken;
            _mobileDevice.SetPreference(PersistKeys.Device.DEVICE_TOKEN, deviceIdentityToken, PersistKeys.Device.SharedName);
        }

        private void CallActivatedCallback(ErrorInfo reason)
        {
            SendErrorIntent("PUSH_ACTIVATE", reason);
        }

        /// <summary>
        /// De-registers the current device.
        /// </summary>
        private async Task Deregister()
        {
            // Make sure the call is not completed synchronously
            await Task.Yield();

            try
            {
                await _restClient.Push.Admin.DeviceRegistrations.RemoveAsync(LocalDevice.Id);
                _ = HandleEvent(new Deregistered());
            }
            catch (AblyException e)
            {
                // Log
                _ = HandleEvent(new DeregistrationFailed(e.ErrorInfo));
            }
        }

        private async Task UpdateRegistration(DeviceDetails details)
        {
            try
            {
                Debug($"Updating device registration {details.ToJson()}");
                await _restClient.Push.Admin.PatchDeviceRecipient(details);
                _ = HandleEvent(new RegistrationSynced());
            }
            catch (AblyException ex)
            {
                Error($"Error updating Registration. DeviceDetails: {details.ToJson()}", ex);
                _ = HandleEvent(new SyncRegistrationFailed(ex.ErrorInfo));
            }
        }

        private LocalDevice LoadPersistedLocalDevice()
        {
            Debug("Loading Local Device persisted state.");
            string GetDeviceSetting(string key) => _mobileDevice.GetPreference(key, PersistKeys.Device.SharedName);

            var localDevice = new LocalDevice();
            string id = GetDeviceSetting(PersistKeys.Device.DEVICE_ID);

            localDevice.Id = id;
            if (id.IsNotEmpty())
            {
                localDevice.DeviceSecret = GetDeviceSetting(PersistKeys.Device.DEVICE_SECRET);
            }

            localDevice.ClientId = GetDeviceSetting(PersistKeys.Device.CLIENT_ID);
            localDevice.DeviceIdentityToken = GetDeviceSetting(PersistKeys.Device.DEVICE_TOKEN);

            var tokenType = GetDeviceSetting(PersistKeys.Device.TOKEN_TYPE);

            if (tokenType.IsNotEmpty())
            {
                string tokenString = GetDeviceSetting(PersistKeys.Device.TOKEN);

                if (tokenString.IsNotEmpty())
                {
                    var token = new RegistrationToken(tokenType, tokenString);
                    localDevice.RegistrationToken = token;
                }
            }

            Debug($"LocalDevice loaded: {localDevice.ToJson()}");

            return localDevice;
        }

        private LocalDevice EnsureLocalDeviceIsLoaded()
        {
            if (LocalDevice.IsCreated == false)
            {
                LocalDevice = LoadPersistedLocalDevice();
            }

            return LocalDevice;
        }

        private void ResetDevice()
        {
            _mobileDevice.ClearPreferences(PersistKeys.Device.SharedName);
            LocalDevice = new LocalDevice();
        }

        private void CallSyncRegistrationFailedCallback(ErrorInfo reason)
        {
            SendErrorIntent("PUSH_UPDATE_FAILED", reason);
        }

        public void LoadPersistedState()
        {
            Debug("Loading persisted state.");

            var canEnter = _handleEventsLock.Wait(1000); // Arbitrary number = 1 second

            if (canEnter == false)
            {
                throw new AblyException("Failed to get ActivationStateMachine state lock.");
            }

            CurrentState = LoadState();
            _pendingEvents = LoadPersistedEvents();

            Debug($"State loaded. CurrentState: '{CurrentState.GetType().Name}', PendingEvents: '{_pendingEvents.Select((x, i) => $"({i}) {x.GetType().Name}").JoinStrings()}'.");

            Queue<Event> LoadPersistedEvents()
            {
                var persistedEvents = _mobileDevice.GetPreference(PersistKeys.StateMachine.PENDING_EVENTS, PersistKeys.StateMachine.SharedName);
                var eventNames = persistedEvents.Split('|');

                return new Queue<Event>(eventNames.Select(ParseEvent).Where(x => x != null));
            }

            Event ParseEvent(string eventName)
            {
                switch (eventName)
                {
                    case nameof(CalledActivate):
                        return new CalledActivate();
                    case nameof(CalledDeactivate):
                        return new CalledDeactivate();
                    case nameof(GotPushDeviceDetails):
                        return new GotPushDeviceDetails();
                    case nameof(RegistrationSynced):
                        return new RegistrationSynced();
                    case nameof(Deregistered):
                        return new Deregistered();
                    default: return null;
                }
            }

            State LoadState()
            {
                var currentState = _mobileDevice.GetPreference(PersistKeys.StateMachine.CURRENT_STATE, PersistKeys.StateMachine.SharedName);
                switch (currentState)
                {
                    case nameof(NotActivated):
                        return new NotActivated(this);
                    case nameof(WaitingForPushDeviceDetails):
                        return new WaitingForPushDeviceDetails(this);
                    case nameof(WaitingForNewPushDeviceDetails):
                        return new WaitingForNewPushDeviceDetails(this);
                    case nameof(AfterRegistrationSyncFailed):
                        return new AfterRegistrationSyncFailed(this);
                    default:
                        return new NotActivated(this);
                }
            }
        }
    }
}
