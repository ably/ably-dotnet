using System.Threading.Tasks;
using IO.Ably;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionSuspendedState : ConnectionStateBase
    {
        private readonly ICountdownTimer _timer;

        public ConnectionSuspendedState(IConnectionContext context, ILogger logger) :
            this(context, null, new CountdownTimer("Suspended state timer", logger), logger)
        {
        }

        public ConnectionSuspendedState(IConnectionContext context, ErrorInfo error, ILogger logger) :
            this(context, error, new CountdownTimer("Suspended state timer", logger), logger)
        {
        }

        public ConnectionSuspendedState(IConnectionContext context, ErrorInfo error, ICountdownTimer timer, ILogger logger) :
            base(context, logger)
        {
            _timer = timer;
            Error = error ?? ErrorInfo.ReasonSuspended;
            RetryIn = context.SuspendRetryTimeout;
        }

        public override Realtime.ConnectionState State => Realtime.ConnectionState.Suspended;

        public override void Connect()
        {
            Context.SetState(new ConnectionConnectingState(Context, Logger));
        }

        public override void Close()
        {
            _timer.Abort();
            Context.SetState(new ConnectionClosedState(Context, Logger));
        }

        public override void AbortTimer()
        {
            _timer.Abort();
        }

        public override Task OnAttachToContext()
        {
            if(RetryIn.HasValue)
                _timer.Start(RetryIn.Value, OnTimeOut);
            return TaskConstants.BooleanTrue;
        }

        private void OnTimeOut()
        {
            Context.Execute(() => Context.SetState(new ConnectionConnectingState(Context, Logger)));
        }
    }
}