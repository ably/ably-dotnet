using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionClosedState : ConnectionStateBase
    {
        public override ErrorInfo DefaultErrorInfo => ErrorInfo.ReasonClosed;

        public ConnectionClosedState(IConnectionContext context)
            : this(context, null)
        {
        }

        public ConnectionClosedState(IConnectionContext context, ErrorInfo error)
            : base(context)
        {
            Error = error ?? ErrorInfo.ReasonClosed;
        }

        public override ConnectionState State => ConnectionState.Closed;

        public override RealtimeCommand Connect()
        {
            return SetConnectingStateCommand.Create();
        }
    }
}
