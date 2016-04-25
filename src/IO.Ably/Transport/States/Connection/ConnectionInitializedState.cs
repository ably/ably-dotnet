using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionInitializedState : ConnectionState
    {
        public ConnectionInitializedState(IConnectionContext context) :
            base(context)
        { }

        protected override bool CanQueueMessages => true;

        public override Realtime.ConnectionState State => Realtime.ConnectionState.Initialized;

        public override void Connect()
        {
            context.SetState(new ConnectionConnectingState(this.context));
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
