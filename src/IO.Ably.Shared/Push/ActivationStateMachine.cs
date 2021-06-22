using System.Collections.Generic;

namespace IO.Ably.Push
{
    internal partial class ActivationStateMachine
    {
        private readonly AblyRest _restClient;
        private readonly ILogger _logger;

        private Queue<Event> PendingEvents { get; set; } = new Queue<Event>();

        internal ActivationStateMachine(AblyRest restClient, ILogger logger)
        {
            _restClient = restClient;
            _logger = logger;
        }

        public LocalDevice LocalDevice { get; set; }

        private void CallDeactivatedCallback(object o)
        {
            throw new System.NotImplementedException();
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
    }




}
