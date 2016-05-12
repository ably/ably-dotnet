using System.Threading.Tasks;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionInitializedState : ConnectionState
    {
        public ConnectionInitializedState(IConnectionContext context) :
            base(context)
        { }

        public override bool CanQueueMessages => true;

        public override Realtime.ConnectionStateType State => Realtime.ConnectionStateType.Initialized;

        public override void Connect()
        {
            Context.SetState(new ConnectionConnectingState(this.Context));
        }

        public override void Close()
        {
            // do nothing
        }

        public override Task<bool> OnMessageReceived(ProtocolMessage message)
        {
            // could not happen
            Logger.Error("Receiving message in disconected state!");
            return TaskConstants.BooleanFalse;
        }

        public override void AbortTimer()
        {
            
        }
    }
}
