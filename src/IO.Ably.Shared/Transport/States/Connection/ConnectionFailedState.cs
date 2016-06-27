using System.Threading.Tasks;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionFailedState : ConnectionStateBase
    {
        public ConnectionFailedState(IConnectionContext context, ErrorInfo error) :
            base(context)
        {
            Error = error ?? ErrorInfo.ReasonFailed;
        }

        public override Realtime.ConnectionState State => Realtime.ConnectionState.Failed;

        public override void Connect()
        {
            Context.SetState(new ConnectionConnectingState(Context));
        }

        public override void BeforeTransition()
        {
            Context.DestroyTransport();
            Context.Connection.Key = null;
            Context.Connection.Id = null;
        }

        public override Task OnAttachToContext()
        {
            // This is a terminal state. Clear the transport.
            Context.ClearAckQueueAndFailMessages(ErrorInfo.ReasonFailed);
            
            return TaskConstants.BooleanTrue;
        }
    }
}