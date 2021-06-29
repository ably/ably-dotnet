namespace DotnetPush.Models
{
    public class StateModel
    {
        public DeviceState Device { get; set; } = new DeviceState();
        public StateMachineState StateMachine { get; set; } = new StateMachineState();
    }

    public class DeviceState
    {
        public string DeviceId { get; set; }
        public string ClientId { get; set; }
        public string DeviceSecret { get; set; }
        public string DeviceToken { get; set; }
        public string TokenType { get; set; }
        public string Token { get; set; }
    }

    public class StateMachineState
    {
        public string CurrentState { get; set; }
        public string PendingEvents { get; set; }
    }
}