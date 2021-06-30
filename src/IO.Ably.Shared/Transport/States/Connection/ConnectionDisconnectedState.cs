using IO.Ably.Infrastructure;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionDisconnectedState : ConnectionStateBase
    {
        private readonly ICountdownTimer _timer;

        public override ErrorInfo DefaultErrorInfo => ErrorInfo.ReasonDisconnected;

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

        public override ConnectionState State => ConnectionState.Disconnected;

        public override bool CanQueue => true;

        public override RealtimeCommand Connect()
        {
           return SetConnectingStateCommand.Create().TriggeredBy("DisconnectedState.Connect()");
        }

        public override void Close()
        {
            AbortTimer();
            Context.ExecuteCommand(SetClosedStateCommand.Create().TriggeredBy("DisconnectedState.Close()"));
        }

        public override void AbortTimer()
        {
            _timer.Abort();
        }

        public override void StartTimer()
        {
            if (RetryInstantly == false)
            {
                _timer.Start(Context.RetryTimeout, OnTimeOut);
            }
        }

        private void OnTimeOut()
        {
            Context.ExecuteCommand(SetConnectingStateCommand.Create().TriggeredBy("DisconnectedState.OnTimeOut()"));
        }
    }
}
