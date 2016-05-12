using System.Threading.Tasks;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionFailedState : ConnectionState
    {
        public ConnectionFailedState(IConnectionContext context, ErrorInfo error) :
            base(context)
        {
            Error = error ?? ErrorInfo.ReasonFailed;
        }

        public override Realtime.ConnectionStateType State => Realtime.ConnectionStateType.Failed;

        protected override bool CanQueueMessages => false;

        public override void Connect()
        {
            Context.SetState(new ConnectionConnectingState(Context));
        }

        public override void Close()
        {
            // does nothing
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

        public override Task OnAttachedToContext()
        {
            // This is a terminal state. Clear the transport.
            Context.DestroyTransport();
            Context.Connection.Key = null;
            return TaskConstants.BooleanTrue;
        }

        
    }
}