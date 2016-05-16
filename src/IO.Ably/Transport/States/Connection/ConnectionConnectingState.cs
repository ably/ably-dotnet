using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionConnectingState : ConnectionState
    {
        private static readonly ISet<HttpStatusCode> FallbackReasons;

        private readonly ICountdownTimer _timer;
        private readonly bool _useFallbackHost;
        

        static ConnectionConnectingState()
        {
            FallbackReasons = new HashSet<HttpStatusCode>
            {
                HttpStatusCode.InternalServerError,
                HttpStatusCode.GatewayTimeout
            };
        }

        public ConnectionConnectingState(IConnectionContext context, bool useFallbackHost = false) :
            this(context, new CountdownTimer("Connecting state timer"), useFallbackHost)
        {
        }

        public ConnectionConnectingState(IConnectionContext context, ICountdownTimer timer, bool useFallbackHost = false)
            :
                base(context)
        {
            _timer = timer;
            _useFallbackHost = useFallbackHost;
        }

        public override Realtime.ConnectionStateType State => Realtime.ConnectionStateType.Connecting;

        public override bool CanQueue => true;

        public override void Connect()
        {
            Logger.Info("Already connecting!");
        }

        public override void Close()
        {
            TransitionState(new ConnectionClosingState(Context));
        }

        public override async Task<bool> OnMessageReceived(ProtocolMessage message)
        {
            switch (message.action)
            {
                case ProtocolMessage.MessageAction.Connected:
                    {
                        if (Context.Transport.State == TransportState.Connected)
                        {
                            var info = new ConnectionInfo(message);
                            TransitionState(new ConnectionConnectedState(Context, info, message.error));
                        }
                        return true;
                    }
                case ProtocolMessage.MessageAction.Disconnected:
                    {
                        ConnectionState nextState;
                        if (Context.ShouldSuspend())
                        {
                            nextState = new ConnectionSuspendedState(Context, message.error);
                        }
                        else
                        {
                            nextState = new ConnectionDisconnectedState(Context, message.error)
                            {
                                RetryInstantly = await ShouldUseFallbackHost(message.error)
                            };
                        }
                        TransitionState(nextState);
                        return true;
                    }
                case ProtocolMessage.MessageAction.Error:
                    {
                        //If the error is a token error do some magic
                        if (Context.ShouldWeRenewToken(message.error))
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
                                TransitionState(new ConnectionFailedState(Context, ex.ErrorInfo));
                                return true;
                            }
                        }

                        if (await ShouldUseFallbackHost(message.error))
                        {
                            Context.Connection.Key = null;
                            TransitionState(new ConnectionDisconnectedState(Context) { RetryInstantly = true });
                            return true;
                        }

                        TransitionState(new ConnectionFailedState(Context, message.error));
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
            Context.Execute(() => Context.HandleConnectingFailure(null));
        }

        private async Task<bool> ShouldUseFallbackHost(ErrorInfo error)
        {
            return error?.statusCode != null &&
                FallbackReasons.Contains(error.statusCode.Value) &&
                await Context.CanConnectToAbly();
        }

        private void TransitionState(ConnectionState newState)
        {
            _timer.Abort();
            Context.SetState(newState);
        }
    }
}