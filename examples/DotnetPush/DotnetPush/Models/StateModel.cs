namespace DotnetPush.Models
{
    /// <summary>
    /// Class to hold the data stored in device storage which can be displayed on the State page.
    /// </summary>
    public class StateModel
    {
        /// <summary>
        /// Property for realtime push LocalDevice state.
        /// </summary>
        public DeviceState Device { get; set; } = new DeviceState();

        /// <summary>
        /// Property for realtime ActivationStateMachine state.
        /// </summary>
        public StateMachineState StateMachine { get; set; } = new StateMachineState();
    }

    /// <summary>
    /// Class to hold LocalDevice state data.
    /// </summary>
    public class DeviceState
    {
        /// <summary>
        /// Device id.
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Client id.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Device secret.
        /// </summary>
        public string DeviceSecret { get; set; }

        /// <summary>
        /// Device token.
        /// </summary>
        public string DeviceToken { get; set; }

        /// <summary>
        /// Token type.
        /// </summary>
        public string TokenType { get; set; }

        /// <summary>
        /// Token value.
        /// </summary>
        public string Token { get; set; }
    }

    /// <summary>
    /// Class to hold ActivationStateMachine state.
    /// </summary>
    public class StateMachineState
    {
        /// <summary>
        /// Stores the class name of the current ActivationStateMachine state.
        /// </summary>
        public string CurrentState { get; set; }

        /// <summary>
        /// Stores a list of pending events.
        /// </summary>
        public string PendingEvents { get; set; }
    }
}
