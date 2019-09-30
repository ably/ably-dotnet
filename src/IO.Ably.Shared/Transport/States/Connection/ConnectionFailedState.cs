using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    using IO.Ably.Realtime;

    internal class ConnectionFailedState : ConnectionStateBase
    {
        public ConnectionFailedState(IConnectionContext context, ErrorInfo error, ILogger logger)
            : base(context, logger)
        {
            Error = error ?? ErrorInfo.ReasonFailed;
        }

        public override ConnectionState State => ConnectionState.Failed;

        public override RealtimeCommand Connect()
        {
            return SetConnectingStateCommand.Create();
        }

        public override Task OnAttachToContext()
        {
            // Moved to here as OnAttach will contain the main logic
            Context.DestroyTransport();

            // This is a terminal state. Clear the transport.
            Context.ClearAckQueueAndFailMessages(ErrorInfo.ReasonFailed);

            return TaskConstants.BooleanTrue;
        }
    }
}
