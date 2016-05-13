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

        public override void Connect()
        {
            Context.SetState(new ConnectionConnectingState(Context));
        }
        
        public override Task OnAttachToContext()
        {
            // This is a terminal state. Clear the transport.
            Context.ClearAckQueueAndFailMessages(ErrorInfo.ReasonFailed);
            Context.DestroyTransport();
            Context.Connection.Key = null;
            return TaskConstants.BooleanTrue;
        }
    }
}