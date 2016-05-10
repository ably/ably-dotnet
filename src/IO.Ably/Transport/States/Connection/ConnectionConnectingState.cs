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
        private volatile bool _suppressTransportEvents;

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

        protected override bool CanQueueMessages => true;

        public override void Connect()
        {
            // do nothing
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
                            TransitionState(new ConnectionConnectedState(Context, info));
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
                                _suppressTransportEvents = true;
                                await Context.CreateTransport();
                                ConnectTransport();
                                return true;
                            }
                            catch (AblyException ex)
                            {
                                Logger.Error("Error trying to renew token.", ex);
                                TransitionState(new ConnectionFailedState(Context, ex.ErrorInfo));
                                return true;
                            }
                            finally
                            {
                                _suppressTransportEvents = false;
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

        public override async Task OnTransportStateChanged(TransportStateInfo state)
        {
            if (_suppressTransportEvents) return;

            if (state.Error != null || state.State == TransportState.Closed)
            {
                ConnectionState nextState;
                if (Context.ShouldSuspend())
                {
                    nextState = new ConnectionSuspendedState(Context);
                }
                else
                {
                    nextState = new ConnectionDisconnectedState(Context, state)
                    {
                        RetryInstantly = state.Error != null && await Context.CanConnectToAbly()
                    };
                }
                TransitionState(nextState);
            }
        }

        public override async Task OnAttachedToContext()
        {
            Context.AttemptConnection();

            await Context.CreateTransport();

            ConnectTransport();
        }

        private void ConnectTransport()
        {
            if (Context.Transport.State != TransportState.Connected)
            {
                Context.Transport.Connect();
                _timer.StartAsync(Context.DefaultTimeout, async () =>
                {
                    ConnectionState nextState;
                    if (Context.ShouldSuspend())
                    {
                        nextState = new ConnectionSuspendedState(Context);
                    }
                    else
                    {
                        nextState = new ConnectionDisconnectedState(Context, ErrorInfo.ReasonTimeout)
                        {
                            RetryInstantly = await Context.CanConnectToAbly()
                        };
                    }
                    
                    Context.SetState(nextState);
                });
            }
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