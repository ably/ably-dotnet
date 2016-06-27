using System.Threading.Tasks;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionInitializedState : ConnectionStateBase
    {
        public ConnectionInitializedState(IConnectionContext context) :
            base(context)
        { }

        public override bool CanQueue => true;

        public override Realtime.ConnectionState State => Realtime.ConnectionState.Initialized;

        public override void Connect()
        {
            Context.SetState(new ConnectionConnectingState(Context));
        }

        public override void AbortTimer()
        {
            
        }
    }
}
