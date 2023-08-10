using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionFailedState : ConnectionStateBase
    {
        public override ErrorInfo DefaultErrorInfo => ErrorInfo.ReasonFailed;

        public ConnectionFailedState(IConnectionContext context, ErrorInfo error)
            : base(context)
        {
            Error = error ?? ErrorInfo.ReasonFailed;
        }

        public override ConnectionState State => ConnectionState.Failed;

        public override RealtimeCommand Connect()
        {
            return SetConnectingStateCommand.Create();
        }
    }
}
