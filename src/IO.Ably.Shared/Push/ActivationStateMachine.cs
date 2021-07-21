using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IO.Ably.Utils;

namespace IO.Ably.Push
{
    internal partial class ActivationStateMachine
    {
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
            _mobileDevice.SetPreference(PersistKeys.Device.DEVICE_ID, localDevice.Id, PersistKeys.Device.SharedName);
            _mobileDevice.SetPreference(PersistKeys.Device.CLIENT_ID, localDevice.ClientId, PersistKeys.Device.SharedName);
            _mobileDevice.SetPreference(PersistKeys.Device.DEVICE_SECRET, localDevice.DeviceSecret, PersistKeys.Device.SharedName);
            _mobileDevice.SetPreference(PersistKeys.Device.DEVICE_TOKEN, localDevice.DeviceIdentityToken, PersistKeys.Device.SharedName);
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
            _mobileDevice.SetPreference(PersistKeys.Device.DEVICE_TOKEN, deviceIdentityToken, PersistKeys.Device.SharedName);
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
    }
}
