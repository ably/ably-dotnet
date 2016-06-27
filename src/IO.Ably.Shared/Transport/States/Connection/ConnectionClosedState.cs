using System.Threading.Tasks;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionClosedState : ConnectionStateBase
    {
        public ConnectionClosedState(IConnectionContext context) :
            this(context, null)
        {
        }

        public ConnectionClosedState(IConnectionContext context, ErrorInfo error) :
            base(context)
        {
            Error = error ?? ErrorInfo.ReasonClosed;
        }

        public override Realtime.ConnectionState State => Realtime.ConnectionState.Closed;

        public override void Connect()
        {
            Context.SetState(new ConnectionConnectingState(Context));
        }

        public override void BeforeTransition()
        {
            // This is a terminal state. Clear the transport.
            Context.Connection.Key = null;
            Context.Connection.Id = null;
            Context.DestroyTransport();
        }

        public override Task OnAttachToContext()
        {
            Context.ClearAckQueueAndFailMessages(ErrorInfo.ReasonClosed);
            return TaskConstants.BooleanTrue;
        }
    }
}