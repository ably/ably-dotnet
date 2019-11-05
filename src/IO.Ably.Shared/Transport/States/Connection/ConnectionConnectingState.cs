using System;
using System.Threading.Tasks;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    using IO.Ably.Realtime;

    internal class ConnectionConnectingState : ConnectionStateBase
    {
        private readonly ICountdownTimer _timer;

        public ConnectionConnectingState(IConnectionContext context, ILogger logger)
            : this(context, new CountdownTimer("Connecting state timer", logger), logger)
        {
        }

        public ConnectionConnectingState(IConnectionContext context, ICountdownTimer timer, ILogger logger)
            : base(context, logger)
        {
            _timer = timer;
        }

        public override ConnectionState State => ConnectionState.Connecting;

        public override bool CanQueue => true;

        public override RealtimeCommand Connect()
        {
            Logger.Debug("Already connecting!");
            return EmptyCommand.Instance;
        }

        public override void Close()
        {
            TransitionState(SetClosingStateCommand.Create());
        }

        public override async Task<bool> OnMessageReceived(ProtocolMessage message, RealtimeState state)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message), "Null message passed to Connection Connecting State");
            }

            switch (message.Action)
            {
                case ProtocolMessage.MessageAction.Connected:
                    {
                        if (Context.Transport.State == TransportState.Connected)
                        {
                            TransitionState(SetConnectedStateCommand.Create(message, false));
                        }

                        return true;
                    }

                case ProtocolMessage.MessageAction.Disconnected:
                    {
                        Context.ExecuteCommand(HandleConnectingFailureCommand.Create(message.Error));
                        return true;
                    }

                case ProtocolMessage.MessageAction.Error:
                    {
                        // If the error is a token error do some magic
                        bool shouldRenew = Context.ShouldWeRenewToken(message.Error, state);
                        if (shouldRenew)
                        {
                            Context.ExecuteCommand(HandleConnectingTokenErrorCommand.Create(message.Error));
                            return true;
                        }

                        if (await Context.CanUseFallBackUrl(message.Error))
                        {
                            Context.ExecuteCommand(HandleConnectingFailureCommand.Create(message.Error, clearConnectionKey: true));
                            return true;
                        }

                        if (message.Error?.IsTokenError == true && !Context.Connection.RestClient.AblyAuth.TokenRenewable)
                        {
                            TransitionState(SetFailedStateCommand.Create(message.Error));
                            return true;
                        }

                        if (message.Error?.IsTokenError == true)
                        {
                            TransitionState(SetDisconnectedStateCommand.Create(message.Error));
                            return true;
                        }

                        TransitionState(SetFailedStateCommand.Create(message.Error));
                        return true;
                    }
            }

            return false;
        }

        public override void AbortTimer()
        {
            _timer.Abort();
        }

        public override void OnAttachToContext()
        {
            _timer.Start(Context.DefaultTimeout, onTimeOut: OnTimeOut);
        }

        private void OnTimeOut()
        {
            Context.ExecuteCommand(HandleConnectingFailureCommand.Create());
        }

        private void TransitionState(RealtimeCommand command)
        {
            _timer.Abort();
            Context.ExecuteCommand(command);
        }
    }
}
