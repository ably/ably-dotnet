using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionDisconnectedState : ConnectionStateBase
    {
        private readonly ICountdownTimer _timer;

        public ConnectionDisconnectedState(IConnectionContext context, ILogger logger) :
            this(context, null, new CountdownTimer("Disconnected state timer", logger), logger)
        {
        }

        public ConnectionDisconnectedState(IConnectionContext context, ErrorInfo error, ILogger logger) :
            this(context, error, new CountdownTimer("Disconnected state timer", logger), logger)
        {
        }

        public ConnectionDisconnectedState(IConnectionContext context, ErrorInfo error, ICountdownTimer timer, ILogger logger) :
            base(context, logger)
        {
            _timer = timer;
            Error = error;
            RetryIn = context.RetryTimeout;
        }

        public bool RetryInstantly { get; set; }

        public override Realtime.ConnectionState State => Realtime.ConnectionState.Disconnected;

        public override bool CanQueue => true;

        public override void Connect()
        {
            Context.SetState(new ConnectionConnectingState(Context, Logger));
        }

        public override void Close()
        {
            AbortTimer();
            Context.SetState(new ConnectionClosedState(Context, Logger));
        }

        public override void AbortTimer()
        {
            _timer.Abort();
        }

        public override Task OnAttachToContext()
        {
            Context.DestroyTransport();

            if(Logger.IsDebug) Logger.Debug("RetryInstantly set to '" + RetryInstantly + "'");
            if (RetryInstantly)
            {
                Context.SetState(new ConnectionConnectingState(Context, Logger));
            }
            else
            {
                _timer.Start(Context.RetryTimeout, OnTimeOut);
            }

            return TaskConstants.BooleanTrue;
        }

        private void OnTimeOut()
        {
            Context.Execute(() => Context.SetState(new ConnectionConnectingState(Context, Logger)));
        }
    }
}