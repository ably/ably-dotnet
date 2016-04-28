using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionClosingState : ConnectionState
    {
        private const int CloseTimeout = 1000;
        private readonly ICountdownTimer _timer;

        public ConnectionClosingState(IConnectionContext context) :
            this(context, null, new CountdownTimer())
        {
        }

        public ConnectionClosingState(IConnectionContext context, ErrorInfo error) :
            this(context, error, new CountdownTimer())
        {
        }

        public ConnectionClosingState(IConnectionContext context, ErrorInfo error, ICountdownTimer timer) :
            base(context)
        {
            _timer = timer;
            Error = error ?? ErrorInfo.ReasonClosed;
        }

        public override Realtime.ConnectionState State => Realtime.ConnectionState.Closing;

        protected override bool CanQueueMessages => false;

        public override void Connect()
        {
            // do nothing
        }

        public override void Close()
        {
            // do nothing
        }

        public override bool OnMessageReceived(ProtocolMessage message)
        {
            switch (message.action)
            {
                case ProtocolMessage.MessageAction.Closed:
                {
                    TransitionState(new ConnectionClosedState(context));
                    return true;
                }
                case ProtocolMessage.MessageAction.Disconnected:
                {
                    TransitionState(new ConnectionDisconnectedState(context, message.error));
                    return true;
                }
                case ProtocolMessage.MessageAction.Error:
                {
                    TransitionState(new ConnectionFailedState(context, message.error));
                    return true;
                }
            }
            return false;
        }

        public override void OnTransportStateChanged(TransportStateInfo state)
        {
            if (state.State == TransportState.Closed)
            {
                TransitionState(new ConnectionClosedState(context));
            }
        }

        public override void OnAttachedToContext()
        {
            if (context.Transport.State == TransportState.Connected)
            {
                context.Transport.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Close));
                _timer.Start(CloseTimeout, () => context.SetState(new ConnectionClosedState(context)));
            }
            else
            {
                context.SetState(new ConnectionClosedState(context));
            }
        }

        private void TransitionState(ConnectionState newState)
        {
            context.SetState(newState);
            _timer.Abort();
        }
    }
}