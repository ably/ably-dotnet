namespace IO.Ably.Push
{
    /// <summary>
    /// Push methods available for the realtime library.
    /// </summary>
    public interface IPushRealtime
    {
        /// <summary>
        /// Push Admin. See <see cref="PushAdmin"/>.
        /// </summary>
        PushAdmin Admin { get; }

        /// <summary>
        /// Activates the current mobile device. TODO: Add info about needed prerequisites.
        /// </summary>
        void Activate();

        /// <summary>
        /// Deactivates the current mobile device.
        /// </summary>
        void Deactivate();
    }

    /// <summary>
    /// Push Apis for Rest and Realtime clients.
    /// </summary>
    public class PushRest : IPushRealtime
    {
        private readonly AblyRest _rest;
        private readonly ILogger _logger;
        private readonly ActivationStateMachine _stateMachine;

        internal PushRest(AblyRest rest, ILogger logger)
        {
            _rest = rest;
            _logger = logger;
            Admin = new PushAdmin(rest, logger);
            _stateMachine = new ActivationStateMachine(rest, IoC.MobileDevice, logger);
        }

        /// <summary>
        /// Admin APIs for Push notifications.
        /// </summary>
        public PushAdmin Admin { get; }

        /// <summary>
        /// Start the push notification device registration process.
        /// </summary>
        void IPushRealtime.Activate()
        {
            _ = _stateMachine.HandleEvent(new ActivationStateMachine.CalledActivate());
        }

        /// <summary>
        /// Starts the push notification device de-registration process.
        /// </summary>
        void IPushRealtime.Deactivate()
        {
            _ = _stateMachine.HandleEvent(new ActivationStateMachine.CalledDeactivate());
        }
    }
}
