using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Utils;

namespace IO.Ably.Push
{
    internal partial class ActivationStateMachine
    {
        private SemaphoreSlim _handleEventsLock = new SemaphoreSlim(1, 1);

        private readonly AblyRest _restClient;
        private readonly IMobileDevice _mobileDevice;
        private readonly ILogger _logger;

        public string ClientId { get; }

        public State CurrentState { get; private set; }

        private Queue<Event> PendingEvents { get; set; } = new Queue<Event>();

        internal ActivationStateMachine(AblyRest restClient, IMobileDevice mobileDevice, ILogger logger = null)
        {
            _restClient = restClient;
            ClientId = _restClient.Auth.ClientId;
            _mobileDevice = mobileDevice;
            _logger = logger ?? restClient.Logger;
        }

        public LocalDevice LocalDevice { get; set; } = new LocalDevice();

        public async Task HandleEvent(Event @event)
        {
            // Handle current event and any consequent events
            // if current event didn't change state put it in pending queue and return
            // finally check the pending event queue and process it following the above rules
            // finally finally - preserve the state
            Debug($"Handling event ({@event.GetType().Name}. CurrentState: {CurrentState.GetType().Name}");

            var canEnter = await _handleEventsLock.WaitAsync(2000); // Arbitrary number = 2 second
            if (canEnter)
            {
                try
                {
                    await HandleInner();
                    PersistState();
                }
                catch (Exception exception)
                {
                    _logger.Error("Error processing handle event.", exception);
                    throw;
                }
                finally
                {
                    _handleEventsLock.Release();
                }
            }
            else
            {
                Debug("Failed to acquire HandleEvent lock.");
            }

            async Task<State> GetNextState(State currentState, Event @eventToProcess)
            {
                if (eventToProcess is null)
                {
                    return currentState;
                }

                var (nextState, nextEventFunc) = await currentState.Transition(@eventToProcess);

                if (nextState == null || ReferenceEquals(nextState, currentState))
                {
                    return currentState;
                }

                var nextEvent = await nextEventFunc();

                if (nextEvent is null)
                {
                    return nextState;
                }

                return await GetNextState(nextState, nextEvent);
            }

            async Task HandleInner()
            {
                if (CurrentState.CanHandleEvent(@event) == false)
                {
                    Debug("No next state returned. Queuing event for later execution.");
                    PendingEvents.Enqueue(@event);
                    return;
                }

                CurrentState = await GetNextState(CurrentState, @event);

                // Once we have updated the state we can get the next event which came from the Update
                // and try to transition the state again.
                while (PendingEvents.Any())
                {
                    Event pending = PendingEvents.Peek();
                    if (pending is null)
                    {
                        break;
                    }

                    if (CurrentState.CanHandleEvent(pending))
                    {
                        Debug($"Processing pending event ({pending.GetType().Name}. CurrentState: {CurrentState.GetType().Name}");

                        // Update the current state based on the event we got.
                        CurrentState = await GetNextState(CurrentState, pending);
                        _ = PendingEvents.Dequeue(); // Remove the message from the queue.
                    }
                    else
                    {
                        Debug(
                            $"({pending.GetType().Name} can't be handled by currentState: {CurrentState.GetType().Name}");
                        break;
                    }
                }
            }
        }

        private void PersistState()
        {
            Debug(
                $"Persisting State and PendingQueue. State: {CurrentState.GetType().Name}. Queue: {PendingEvents.Select((x, i) => $"({i}) {x.GetType().Name}").JoinStrings()}");

            if (CurrentState != null && CurrentState.Persist)
            {
                _mobileDevice.SetPreference(PersistKeys.StateMachine.CurrentState, CurrentState.GetType().Name, PersistKeys.StateMachine.SharedName);
            }

            var events = PendingEvents.ToList();

            // Saves pending events as a pipe separated list.
            _mobileDevice.SetPreference(PersistKeys.StateMachine.PendingEvents, events.Select(x => x.GetType().Name).JoinStrings("|"), PersistKeys.StateMachine.SharedName);
        }

        private void TriggerDeactivatedCallback(ErrorInfo reason = null)
        {
            if (_mobileDevice.Callbacks.DeactivatedCallback != null)
            {
                _ = NotifyExternalClient(
                    () => _mobileDevice.Callbacks.DeactivatedCallback(reason),
                    nameof(_mobileDevice.Callbacks.DeactivatedCallback));
            }
        }

        private void TriggerActivatedCallback(ErrorInfo reason = null)
        {
            if (_mobileDevice.Callbacks.ActivatedCallback != null)
            {
                NotifyExternalClient(
                    () => _mobileDevice.Callbacks.ActivatedCallback(reason),
                    nameof(_mobileDevice.Callbacks.ActivatedCallback));
            }
        }

        private void TriggerSyncRegistrationFailedCallback(ErrorInfo reason)
        {
            if (_mobileDevice.Callbacks.SyncRegistrationFailedCallback != null)
            {
                NotifyExternalClient(
                    () => _mobileDevice.Callbacks.SyncRegistrationFailedCallback(reason),
                    nameof(_mobileDevice.Callbacks.SyncRegistrationFailedCallback));
            }
        }

        private Task NotifyExternalClient(Func<Task> action, string reason)
        {
            try
            {
                Debug($"Triggering callback {reason}");
                return Task.Run(() => ActionUtils.SafeExecute(action));
            }
            catch (Exception e)
            {
                Error("Error while notifying external client for " + reason, e);
            }

            return Task.CompletedTask;
        }

        protected virtual async Task<Event> ValidateRegistration()
        {
            Debug("Validating Registration");

            // TODO: See if I need to get Ably from some kind of context
            var presentClientId = _restClient.Auth.ClientId;
            if (presentClientId.IsNotEmpty() && LocalDevice.ClientId.IsNotEmpty() &&
                presentClientId.EqualsTo(LocalDevice.ClientId) == false)
            {
                Debug(
                    $"Activation failed. Auth clientId '{presentClientId}' is not equal to Device clientId '{LocalDevice.ClientId}'");

                var error = new ErrorInfo(
                    "Activation failed: present clientId is not compatible with existing device registration",
                    ErrorCodes.ActivationFailedClientIdMismatch,
                    HttpStatusCode.BadRequest);

                return new SyncRegistrationFailed(error);
            }

            try
            {
                await _restClient.Push.Admin.DeviceRegistrations.SaveAsync(LocalDevice);

                return new RegistrationSynced();
            }
            catch (AblyException e)
            {
                Error("Error validating registration", e);
                return new SyncRegistrationFailed(e.ErrorInfo);
            }
        }

        private void Debug(string message) => _logger.Debug($"ActivationStateMachine: {message}");

        private void Error(string message, Exception ex) => _logger.Error($"ActivationStateMachine: {message}", ex);

        internal void PersistLocalDevice(LocalDevice localDevice)
        {
            _mobileDevice.SetPreference(PersistKeys.Device.DeviceId, localDevice.Id, PersistKeys.Device.SharedName);
            _mobileDevice.SetPreference(PersistKeys.Device.ClientId, localDevice.ClientId, PersistKeys.Device.SharedName);
            _mobileDevice.SetPreference(PersistKeys.Device.DeviceSecret, localDevice.DeviceSecret, PersistKeys.Device.SharedName);
            _mobileDevice.SetPreference(PersistKeys.Device.DeviceToken, localDevice.DeviceIdentityToken, PersistKeys.Device.SharedName);
        }

        private void ResetDevice()
        {
            _mobileDevice.ClearPreferences(PersistKeys.Device.SharedName);
            LocalDevice = new LocalDevice();
        }

        private void GetRegistrationToken()
        {
            _mobileDevice.RequestRegistrationToken(UpdateRegistrationToken);
        }

        public void UpdateRegistrationToken(Result<RegistrationToken> tokenResult)
        {
            throw new NotImplementedException();
        }

        private void SetDeviceIdentityToken(string deviceIdentityToken)
        {
            LocalDevice.DeviceIdentityToken = deviceIdentityToken;
            _mobileDevice.SetPreference(PersistKeys.Device.DeviceToken, deviceIdentityToken, PersistKeys.Device.SharedName);
        }

        private LocalDevice EnsureLocalDeviceIsLoaded()
        {
            if (LocalDevice.IsCreated == false)
            {
                LocalDevice = LoadPersistedLocalDevice();
            }

            return LocalDevice;
        }

        internal LocalDevice LoadPersistedLocalDevice()
        {
            Debug("Loading Local Device persisted state.");
            string GetDeviceSetting(string key) => _mobileDevice.GetPreference(key, PersistKeys.Device.SharedName);

            var localDevice = new LocalDevice();
            localDevice.Platform = _mobileDevice.DevicePlatform;
            localDevice.FormFactor = _mobileDevice.FormFactor;
            string id = GetDeviceSetting(PersistKeys.Device.DeviceId);

            localDevice.Id = id;
            if (id.IsNotEmpty())
            {
                localDevice.DeviceSecret = GetDeviceSetting(PersistKeys.Device.DeviceSecret);
            }

            localDevice.ClientId = GetDeviceSetting(PersistKeys.Device.ClientId);
            localDevice.DeviceIdentityToken = GetDeviceSetting(PersistKeys.Device.DeviceToken);

            var tokenType = GetDeviceSetting(PersistKeys.Device.TokenType);

            if (tokenType.IsNotEmpty())
            {
                string tokenString = GetDeviceSetting(PersistKeys.Device.Token);

                if (tokenString.IsNotEmpty())
                {
                    var token = new RegistrationToken(tokenType, tokenString);
                    localDevice.RegistrationToken = token;
                }
            }

            Debug($"LocalDevice loaded: {localDevice.ToJson()}");

            return localDevice;
        }
    }
}
