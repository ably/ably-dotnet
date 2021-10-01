using System;
using System.Threading.Tasks;

namespace IO.Ably.Push
{
    /// <summary>
    /// Push Apis for Realtime clients.
    /// </summary>
    public class PushRealtime : IDisposable
    {
        private readonly AblyRest _restClient;
        private readonly ILogger _logger;

        private ActivationStateMachine StateMachine { get; set; }

        internal PushRealtime(AblyRest restClient, ILogger logger)
        {
            _restClient = restClient;
            _logger = logger;
        }

        internal void InitialiseStateMachine()
        {
            if (_restClient.MobileDevice != null)
            {
                StateMachine = new ActivationStateMachine(_restClient, _logger);
                if (_restClient.Device != null)
                {
                    _restClient.Device.ClientIdUpdated = ClientIdUpdated;
                }
            }
        }

        internal void ClientIdUpdated(string newClientId)
        {
            var currentStateIsNotNotActivated =
                (StateMachine.CurrentState is ActivationStateMachine.NotActivated) == false;
            if (_restClient.Device.IsRegistered && currentStateIsNotNotActivated)
            {
                _ = Task.Run(() => StateMachine.HandleEvent(new ActivationStateMachine.GotPushDeviceDetails()));
            }
        }

        /// <summary>
        /// Start the push notification device registration process.
        /// </summary>
        public void Activate()
        {
            if (StateMachine is null)
            {
                throw new AblyException("Realtime push is not enabled. Please initialise Ably by calling (AblyAndroidMobileDevice / AblyAppleMobileDevice).Initialize() before calling `Activate`");
            }
        }

        /// <summary>
        /// Starts the push notification device de-registration process.
        /// </summary>
        public void Deactivate()
        {
            if (StateMachine is null)
            {
                throw new AblyException("Realtime push is not enabled. Please initialise Ably by calling (AblyAndroidMobileDevice / AblyAppleMobileDevice).Initialize() before calling `Deactivate`");
            }
        }

        /// <summary>
        /// Admin APIs for Push notifications.
        /// </summary>
        public PushAdmin Admin => _restClient.Push.Admin;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_restClient.Device != null)
            {
                _restClient.Device.ClientIdUpdated = null;
            }
        }
    }
}
