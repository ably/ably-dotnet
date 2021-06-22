using System.Collections.Generic;

namespace IO.Ably.Push
{
    internal partial class ActivationStateMachine
    {
        private readonly AblyRest _restClient;
        private readonly IMobileDevice _mobileDevice;
        private readonly ILogger _logger;

        private Queue<Event> PendingEvents { get; set; } = new Queue<Event>();

        internal ActivationStateMachine(AblyRest restClient, IMobileDevice mobileDevice, ILogger logger)
        {
            _restClient = restClient;
            _mobileDevice = mobileDevice;
            _logger = logger;
        }

        public LocalDevice LocalDevice { get; set; }

        private void CallDeactivatedCallback(ErrorInfo reason)
        {
            SendErrorIntent("PUSH_DEACTIVATE", reason); // TODO: Put intent names in consts
        }

        private void ValidateRegistration()
        {
            throw new System.NotImplementedException();
        }

        private void AddToEventQueue(Event @event)
        {
            PendingEvents.Enqueue(@event);
        }

        private void GetRegistrationToken()
        {
            throw new System.NotImplementedException();
        }

        private void CreateLocalDevice()
        {
            throw new System.NotImplementedException();
        }

        private void SendErrorIntent(string name, ErrorInfo errorInfo)
        {
            var properties = new Dictionary<string, object>();
            properties.Add("hasError", errorInfo != null);
            if (errorInfo != null) {
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
