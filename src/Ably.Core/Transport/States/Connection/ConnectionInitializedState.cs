using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionInitializedState : ConnectionState
    {
        public ConnectionInitializedState(IConnectionContext context) :
            base(context)
        { }

        protected override bool CanQueueMessages
        {
            get
            {
                return true;
            }
        }

        public override Realtime.ConnectionState State
        {
            get
            {
                return Realtime.ConnectionState.Initialized;
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
            // do nothing
            return false;
        }

        public override void OnTransportStateChanged(TransportStateInfo state)
        {
            // do nothing
        }
    }
}
