using System;
using System.Threading.Tasks;
using IO.Ably;
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

        public override ConnectionState State => Realtime.ConnectionState.Connecting;

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

        public override async ValueTask<bool> OnMessageReceived(ProtocolMessage message, RealtimeState state)
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
                        Context.HandleConnectingFailure(message.Error, null);
                        return true;
                    }

                case ProtocolMessage.MessageAction.Error:
                    {
                        // If the error is a token error do some magic
                        bool shouldRenew = Context.ShouldWeRenewToken(message.Error);
                        if (shouldRenew)
                        {
                            try
                            {
                                Context.ClearTokenAndRecordRetry();
                                await Context.CreateTransport();
                                return true;
                            }
                            catch (AblyException ex)
                            {
                                Logger.Error("Error trying to renew token.", ex);
                                TransitionState(SetDisconnectedStateCommand.Create(ex.ErrorInfo));
                                return true;
                            }
                        }

                        if (await Context.CanUseFallBackUrl(message.Error))
                        {
                            Context.Connection.Key = null;
                            Context.HandleConnectingFailure(message.Error, null);
                            return true;
                        }

                        if (message.Error?.IsTokenError == true && !Context.Connection.RestClient.AblyAuth.TokenRenewable)
                        {
                            TransitionState(SetFailedStateCommand.Create(message.Error));
                            return true;
                        }

                        if (message.Error?.IsTokenError == true && !shouldRenew) //Always true. Cleanup!
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

        public override async Task OnAttachToContext()
        {
            // RTN15g - If a client has been disconnected for longer
            // than the connectionStateTtl, it should not attempt to resume.
            if (Context.Connection.ConfirmedAliveAt?.Add(Context.Connection.ConnectionStateTtl) < DateTimeOffset.UtcNow)
            {
                Context.Connection.Id = string.Empty;
                Context.Connection.Key = string.Empty;
            }

            await Context.CreateTransport();
            _timer.Start(Context.DefaultTimeout, onTimeOut: OnTimeOut);
        }

        private void OnTimeOut()
        {
            Context.HandleConnectingFailure(null, null);
        }

        private void TransitionState(RealtimeCommand command)
        {
            _timer.Abort();
            Context.ExecuteCommand(command);
        }
    }
}
