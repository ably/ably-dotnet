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

        internal ActivationStateMachine(AblyRest restClient, IMobileDevice mobileDevice, ILogger logger)
        {
            _restClient = restClient;
            ClientId = _restClient.Auth.ClientId;
            _mobileDevice = mobileDevice;
            _logger = logger;
        }

        public LocalDevice LocalDevice { get; set; } = new LocalDevice();

        private void TriggerDeactivatedCallback()
        {
            ActionUtils.SafeExecute(() => _mobileDevice.Callbacks.DeactivatedCallback?.Invoke(null), _logger);
        }

        private async Task<Event> ValidateRegistration()
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

            return null;
        }

        private void Debug(string message) => _logger.Debug($"ActivationStateMachine: {message}");

        private void Error(string message, Exception ex) => _logger.Error($"ActivationStateMachine: {message}", ex);
    }
}
