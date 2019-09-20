using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Realtime.Workflow;

namespace IO.Ably.Transport.States.Connection
{
    using IO.Ably.Realtime;

    internal class ConnectionSuspendedState : ConnectionStateBase
    {
        private readonly ICountdownTimer _timer;

        public new ErrorInfo DefaultErrorInfo => ErrorInfo.ReasonSuspended;

        public ConnectionSuspendedState(IConnectionContext context, ILogger logger)
            : this(context, null, new CountdownTimer("Suspended state timer", logger), logger)
        {
        }

        public ConnectionSuspendedState(IConnectionContext context, ErrorInfo error, ILogger logger)
            : this(context, error, new CountdownTimer("Suspended state timer", logger), logger)
        {
        }

        public ConnectionSuspendedState(IConnectionContext context, ErrorInfo error, ICountdownTimer timer, ILogger logger)
            : base(context, logger)
        {
            _timer = timer;
            Error = error ?? ErrorInfo.ReasonSuspended;
            RetryIn = context.SuspendRetryTimeout;
        }

        public override ConnectionState State => Realtime.ConnectionState.Suspended;

        public override void Connect()
        {
            Context.ExecuteCommand(SetConnectingStateCommand.Create());
        }

        public override void Close()
        {
            _timer.Abort();
            Context.ExecuteCommand(SetClosedStateCommand.Create());
        }

        public override void AbortTimer()
        {
            _timer.Abort();
        }

        public override Task OnAttachToContext()
        {
            // This is a terminal state. Clear the transport.
            Context.ClearAckQueueAndFailMessages(ErrorInfo.ReasonSuspended);

            if (RetryIn.HasValue)
            {
                _timer.Start(RetryIn.Value, OnTimeOut);
            }

            return TaskConstants.BooleanTrue;
        }

        private void OnTimeOut()
        {
            Context.ExecuteCommand(SetConnectingStateCommand.Create());
        }
    }
}
