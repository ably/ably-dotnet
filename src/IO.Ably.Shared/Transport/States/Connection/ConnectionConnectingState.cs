using System;
using System.Threading.Tasks;
using IO.Ably;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionConnectingState : ConnectionStateBase
    {
        private readonly ICountdownTimer _timer;

        public ConnectionConnectingState(IConnectionContext context, ILogger logger) :
            this(context, new CountdownTimer("Connecting state timer", logger), logger)
        {
        }

        public ConnectionConnectingState(IConnectionContext context, ICountdownTimer timer, ILogger logger)
            : base(context, logger)
        {
            _timer = timer;
        }

        public override Realtime.ConnectionState State => Realtime.ConnectionState.Connecting;

        public override bool CanQueue => true;

        public override void Connect()
        {
            Logger.Debug("Already connecting!");
        }

        public override void Close()
        {
            TransitionState(new ConnectionClosingState(Context, Logger));
        }

        public override async Task<bool> OnMessageReceived(ProtocolMessage message)
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
                            var info = new ConnectionInfo(message);
                            TransitionState(new ConnectionConnectedState(Context, info, message.Error, Logger));
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
                        //If the error is a token error do some magic
                        if (Context.ShouldWeRenewToken(message.Error))
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
                                TransitionState(new ConnectionFailedState(Context, ex.ErrorInfo, Logger));
                                return true;
                            }
                        }

                        if (await Context.CanUseFallBackUrl(message.Error))
                        {
                            Context.Connection.Key = null;
                            Context.HandleConnectingFailure(message.Error, null);
                            return true;
                        }

                        TransitionState(new ConnectionFailedState(Context, message.Error, Logger));
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
            await Context.CreateTransport();
            _timer.Start(Context.DefaultTimeout, onTimeOut: OnTimeOut);
        }

        private void OnTimeOut()
        {
            Context.Execute(() => Context.HandleConnectingFailure(null, null));
        }

        private void TransitionState(ConnectionStateBase newState)
        {
            _timer.Abort();
            Context.SetState(newState);
        }
    }
}