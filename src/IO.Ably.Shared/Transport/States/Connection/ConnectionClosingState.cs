using System;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionClosingState : ConnectionStateBase
    {
        private const int CloseTimeout = 1000;
        private readonly bool _connectedTransport;
        private readonly ICountdownTimer _timer;

        public ConnectionClosingState(IConnectionContext context, bool connectedTransport,ILogger logger)
            : this(context, null, connectedTransport, new CountdownTimer("Closing state timer", logger), logger)
        {
        }

        public ConnectionClosingState(IConnectionContext context, ErrorInfo error, bool connectedTransport, ICountdownTimer timer, ILogger logger)
            : base(context, logger)
        {
            _connectedTransport = connectedTransport;
            _timer = timer;
            Error = error ?? ErrorInfo.ReasonClosed;
        }

        public override ConnectionState State => ConnectionState.Closing;

        public override async ValueTask<bool> OnMessageReceived(ProtocolMessage message, RealtimeState state)
        {
            switch (message.Action)
            {
                case ProtocolMessage.MessageAction.Closed:
                    TransitionState(SetClosedStateCommand.Create());
                    return true;
                case ProtocolMessage.MessageAction.Disconnected:
                    TransitionState(SetDisconnectedStateCommand.Create(message.Error));
                    return true;
                case ProtocolMessage.MessageAction.Error:
                    TransitionState(SetFailedStateCommand.Create(message.Error));
                    return true;
            }

            return false;
        }

        private void TransitionState(RealtimeCommand command)
        {
            _timer.Abort();
            Context.ExecuteCommand(command);
        }

        public override void AbortTimer()
        {
            _timer.Abort();
        }

        public override void OnAttachToContext()
        {
            if (_connectedTransport)
            {
                _timer.Start(TimeSpan.FromMilliseconds(CloseTimeout), OnTimeOut);
            }
        }

        private void OnTimeOut()
        {
            Context.ExecuteCommand(SetClosedStateCommand.Create());
        }

        public override RealtimeCommand Connect()
        {
            _timer.Abort();
            return SetConnectingStateCommand.Create(clearConnectionKey: true);
        }
    }
}
