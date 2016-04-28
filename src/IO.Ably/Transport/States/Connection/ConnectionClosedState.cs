using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionClosedState : ConnectionState
    {
        public ConnectionClosedState(IConnectionContext context) :
            this(context, null)
        { }

        public ConnectionClosedState(IConnectionContext context, ErrorInfo error) :
            base(context)
        {
            this.Error = error ?? ErrorInfo.ReasonClosed;
        }

        public override Realtime.ConnectionState State
        {
            get
            {
                return Realtime.ConnectionState.Closed;
            }
        }

        protected override bool CanQueueMessages
        {
            get
            {
                return false;
            }
        }

        public override void Connect()
        {
            this.context.SetState(new ConnectionConnectingState(this.context));
        }

        public override void Close()
        {
            // do nothing
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
            // This is a terminal state. Clear the transport.
            this.context.DestroyTransport();
            this.context.Connection.Key = null;
        }
    }
}
