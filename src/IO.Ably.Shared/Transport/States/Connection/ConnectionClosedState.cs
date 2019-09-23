using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    using IO.Ably.Realtime;

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

        public override ConnectionState State => Realtime.ConnectionState.Closed;

        public override RealtimeCommand Connect()
        {
            return SetConnectingStateCommand.Create();
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
