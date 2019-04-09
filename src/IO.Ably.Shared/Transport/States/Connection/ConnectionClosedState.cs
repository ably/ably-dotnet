using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionClosedState : ConnectionStateBase
    {
        public new ErrorInfo DefaultErrorInfo => ErrorInfo.ReasonClosed;

        public ConnectionClosedState(IConnectionContext context, ILogger logger)
            : this(context, null, logger)
        {
        }

        public ConnectionClosedState(IConnectionContext context, ErrorInfo error, ILogger logger)
            : base(context, logger)
        {
            Error = error ?? ErrorInfo.ReasonClosed;
        }

        public override Realtime.ConnectionState State => Realtime.ConnectionState.Closed;

        public override void Connect()
        {
            Context.SetState(new ConnectionConnectingState(Context, Logger));
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
