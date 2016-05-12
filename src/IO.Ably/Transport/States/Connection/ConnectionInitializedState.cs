using System.Threading.Tasks;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionInitializedState : ConnectionState
    {
        public ConnectionInitializedState(IConnectionContext context) :
            base(context)
        { }

        public override bool CanQueue => true;

        public override Realtime.ConnectionStateType State => Realtime.ConnectionStateType.Initialized;

        public override void Connect()
        {
            Context.SetState(new ConnectionConnectingState(this.Context));
        }

        public override void AbortTimer()
        {
            
        }
    }
}
