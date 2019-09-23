using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    using Realtime;

    internal class ConnectionDisconnectedState : ConnectionStateBase
    {
        private readonly ICountdownTimer _timer;

        public new ErrorInfo DefaultErrorInfo => ErrorInfo.ReasonDisconnected;

        public ConnectionDisconnectedState(IConnectionContext context, ILogger logger)
            : this(context, null, new CountdownTimer("Disconnected state timer", logger), logger)
        {
        }

        public ConnectionDisconnectedState(IConnectionContext context, ErrorInfo error, ILogger logger)
            : this(context, error, new CountdownTimer("Disconnected state timer", logger), logger)
        {
        }

        public ConnectionDisconnectedState(IConnectionContext context, ErrorInfo error, ICountdownTimer timer, ILogger logger)
            : base(context, logger)
        {
            _timer = timer;
            Error = error;
            RetryIn = context.RetryTimeout;
        }

        public bool RetryInstantly { get; set; }

        public override ConnectionState State => Realtime.ConnectionState.Disconnected;

        public override bool CanQueue => true;

        public override RealtimeCommand Connect()
        {
           return SetConnectingStateCommand.Create();
        }

        public override void Close()
        {
            AbortTimer();
            Context.ExecuteCommand(SetClosedStateCommand.Create());
        }

        public override void AbortTimer()
        {
            _timer.Abort();
        }

        public override async Task OnAttachToContext()
        {
            Context.DestroyTransport();

            if (Logger.IsDebug)
            {
                Logger.Debug("RetryInstantly set to '" + RetryInstantly + "'");
            }

            if (RetryInstantly)
            {
                Context.ExecuteCommand(SetConnectingStateCommand.Create());
            }
            else
            {
                _timer.Start(Context.RetryTimeout, OnTimeOut);
            }
        }

        private void OnTimeOut()
        {
            Context.ExecuteCommand(SetConnectingStateCommand.Create());
        }
    }
}
