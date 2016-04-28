using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionDisconnectedState : ConnectionState
    {
        private const int ConnectTimeout = 15*1000;
        private readonly ICountdownTimer _timer;

        public ConnectionDisconnectedState(IConnectionContext context) :
            this(context, null, new CountdownTimer())
        {
        }

        public ConnectionDisconnectedState(IConnectionContext context, TransportStateInfo stateInfo) :
            this(context, CreateError(stateInfo), new CountdownTimer())
        {
        }

        public ConnectionDisconnectedState(IConnectionContext context, ErrorInfo error) :
            this(context, error, new CountdownTimer())
        {
        }

        public ConnectionDisconnectedState(IConnectionContext context, ErrorInfo error, ICountdownTimer timer) :
            base(context)
        {
            _timer = timer;
            Error = error ?? ErrorInfo.ReasonDisconnected;
            RetryIn = ConnectTimeout; //TODO: Make sure this comes from ClientOptions
        }

        public bool UseFallbackHost { get; set; }

        public override Realtime.ConnectionState State => Realtime.ConnectionState.Disconnected;

        protected override bool CanQueueMessages => true;

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
            if (UseFallbackHost)
            {
                context.SetState(new ConnectionConnectingState(context));
            }
            else
            {
                _timer.Start(ConnectTimeout, OnTimeOut);
            }
        }

        private void OnTimeOut()
        {
            context.SetState(new ConnectionConnectingState(context));
        }

        private static ErrorInfo CreateError(TransportStateInfo state)
        {
            return ErrorInfo.ReasonDisconnected;
        }
    }
}