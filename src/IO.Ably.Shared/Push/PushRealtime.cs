namespace IO.Ably.Push
{
    /// <summary>
    /// Push Apis for Realtime clients.
    /// </summary>
    public class PushRealtime
    {
        private readonly AblyRest _restClient;
        private readonly ActivationStateMachine _stateMachine;

        internal PushRealtime(AblyRest restClient, ILogger logger)
        {
            _restClient = restClient;
            if (_restClient.MobileDevice != null)
            {
                _stateMachine = new ActivationStateMachine(restClient, logger);
            }
        }

        /// <summary>
        /// Start the push notification device registration process.
        /// </summary>
        public void Activate()
        {
            if (_stateMachine is null)
            {
                throw new AblyException("Realtime push is not enabled. Please call (AndroidMobileDevice / IOSMobileDevice).Initialize() before calling `Activate`"); // TODO: Come up with a better message.
            }
        }

        /// <summary>
        /// Starts the push notification device de-registration process.
        /// </summary>
        public void Deactivate()
        {
            if (_stateMachine is null)
            {
                throw new AblyException("Realtime push is not enabled. Please call (AndroidMobileDevice / IOSMobileDevice).Initialize() before calling `Deactivate`"); // TODO: Come up with a better message.
            }
        }

        /// <summary>
        /// Admin APIs for Push notifications.
        /// </summary>
        public PushAdmin Admin => _restClient.Push.Admin;
    }
}
