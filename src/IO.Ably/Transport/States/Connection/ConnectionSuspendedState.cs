using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionSuspendedState : ConnectionState
    {
        //TODO: Make sure these come from config
        public const int SuspendTimeout = 120*1000; // Time before a connection is considered suspended
        private const int ConnectTimeout = 120*1000; // Time to wait before retrying connection
        private readonly ICountdownTimer _timer;

        public ConnectionSuspendedState(IConnectionContext context) :
            this(context, null, new CountdownTimer())
        {
        }

        public ConnectionSuspendedState(IConnectionContext context, ErrorInfo error) :
            this(context, error, new CountdownTimer())
        {
        }

        public ConnectionSuspendedState(IConnectionContext context, ErrorInfo error, ICountdownTimer timer) :
            base(context)
        {
            _timer = timer;
            Error = error ?? ErrorInfo.ReasonSuspended;
            RetryIn = ConnectTimeout;
        }

        public override Realtime.ConnectionState State => Realtime.ConnectionState.Suspended;

        protected override bool CanQueueMessages => false;

        public override void Connect()
        {
            context.SetState(new ConnectionConnectingState(context));
        }

        public override void Close()
        {
            context.SetState(new ConnectionClosedState(context));
        }

        public override bool OnMessageReceived(ProtocolMessage message)
        {
            // could not happen
            return false;
        }

        public override void OnTransportStateChanged(TransportStateInfo state)
        {
            // could not happen
        }

        public override void OnAttachedToContext()
        {
            _timer.Start(ConnectTimeout, OnTimeOut);
        }

        private void OnTimeOut()
        {
            context.SetState(new ConnectionConnectingState(context));
        }
    }
}