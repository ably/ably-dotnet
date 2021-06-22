using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using IO.Ably.Encryption;

namespace IO.Ably.Push
{
    internal partial class ActivationStateMachine
    {
        private readonly AblyRest _restClient;
        private readonly IMobileDevice _mobileDevice;
        private readonly ILogger _logger;

        public string ClientId { get; }

        private Queue<Event> PendingEvents { get; set; } = new Queue<Event>();

        internal ActivationStateMachine(AblyRest restClient, IMobileDevice mobileDevice, ILogger logger)
        {
            _restClient = restClient;
            ClientId = _restClient.Auth.ClientId;
            _mobileDevice = mobileDevice;
            _logger = logger;
        }

        public LocalDevice LocalDevice { get; set; } = new LocalDevice();

        private void CallDeactivatedCallback(ErrorInfo reason)
        {
            SendErrorIntent("PUSH_DEACTIVATE", reason); // TODO: Put intent names in consts
        }

        private async Task ValidateRegistration()
        {
            // TODO: See if I need to get Ably from some kind of context
            var presentClientId = _restClient.Auth.ClientId;
            if (presentClientId.IsNotEmpty() && presentClientId.EqualsTo(LocalDevice.ClientId) == false)
            {
                var error = new ErrorInfo(
                    "Activation failed: present clientId is not compatible with existing device registration",
                    ErrorCodes.ActivationFailedClientIdMismatch,
                    HttpStatusCode.BadRequest);

                // When calling Handle event we don't want to await the operation.
                // I'm sure there is a better way to do it.
                _ = HandleEvent(new SyncRegistrationFailed(error));
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

        private async Task HandleEvent(Event @event)
        {
            throw new NotImplementedException();
        }

        private void AddToEventQueue(Event @event)
        {
            PendingEvents.Enqueue(@event);
        }

        private void GetRegistrationToken()
        {
            // TODO: Will be it's own PR.
            throw new System.NotImplementedException();
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
    }
}
