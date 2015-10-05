using Ably.Types;
using System;
using System.Collections.Generic;

namespace Ably.Transport.States.Connection
{
    internal class ConnectionConnectingState : ConnectionState
    {
        static ConnectionConnectingState()
        {
            FallbackReasons = new HashSet<System.Net.HttpStatusCode>()
            {
                System.Net.HttpStatusCode.InternalServerError,
                System.Net.HttpStatusCode.GatewayTimeout
            };
        }

        public ConnectionConnectingState(IConnectionContext context, bool useFallbackHost = false) :
            this(context, new CountdownTimer(), useFallbackHost)
        { }

        public ConnectionConnectingState(IConnectionContext context, ICountdownTimer timer, bool useFallbackHost = false) :
            base(context)
        {
            _timer = timer;
            _useFallbackHost = useFallbackHost;
        }

        private const int ConnectTimeout = 15 * 1000;
        private static readonly ISet<System.Net.HttpStatusCode> FallbackReasons;

        private ICountdownTimer _timer;
        private bool _useFallbackHost;

        public override Realtime.ConnectionState State
        {
            get
            {
                return Realtime.ConnectionState.Connecting;
            }
        }

        protected override bool CanQueueMessages
        {
            get
            {
                return true;
            }
        }

        public override void Connect()
        {
            // do nothing
        }

        public override void Close()
        {
            this.TransitionState(new ConnectionClosingState(this.context));
        }

        public override bool OnMessageReceived(ProtocolMessage message)
        {
            switch (message.Action)
            {
                case ProtocolMessage.MessageAction.Connected:
                    {
                        if (context.Transport.State == TransportState.Connected)
                        {
                            ConnectionInfo info = new ConnectionInfo(message.ConnectionId, message.ConnectionSerial ?? -1, message.ConnectionKey);
                            this.TransitionState(new ConnectionConnectedState(this.context, info));
                        }
                        return true;
                    }
                case ProtocolMessage.MessageAction.Disconnected:
                    {
                        ConnectionState nextState;
                        if (this.ShouldSuspend())
                        {
                            nextState = new ConnectionSuspendedState(this.context, message.Error);
                        }
                        else
                        {
                            nextState = new ConnectionDisconnectedState(this.context, message.Error)
                            {
                                UseFallbackHost = ShouldUseFallbackHost(message.Error)
                            };
                        }
                        this.TransitionState(nextState);
                        return true;
                    }
                case ProtocolMessage.MessageAction.Error:
                    {
                        if (ShouldUseFallbackHost(message.Error))
                        {
                            this.context.Connection.Key = null;
                            this.TransitionState(new ConnectionDisconnectedState(this.context) { UseFallbackHost = true });
                        }
                        this.TransitionState(new ConnectionFailedState(this.context, message.Error));
                        return true;
                    }
            }
            return false;
        }

        public override void OnTransportStateChanged(TransportStateInfo state)
        {
            if (state.Error != null || state.State == TransportState.Closed)
            {
                ConnectionState nextState;
                if (this.ShouldSuspend())
                {
                    nextState = new ConnectionSuspendedState(this.context);
                }
                else
                {
                    nextState = new ConnectionDisconnectedState(this.context, state)
                    {
                        UseFallbackHost = state.Error != null && AblyRealtime.CanConnectToAbly()
                    };
                }
                this.TransitionState(nextState);
            }
        }

        public override void OnAttachedToContext()
        {
            context.AttemptConnection();

            if (context.Transport == null || _useFallbackHost)
            {
                context.CreateTransport(_useFallbackHost);
            }

            if (context.Transport.State != TransportState.Connected)
            {
                this.context.Transport.Connect();
                _timer.Start(ConnectTimeout, () =>
                {
                    this.context.SetState(new ConnectionDisconnectedState(this.context, ErrorInfo.ReasonTimeout)
                    {
                        UseFallbackHost = AblyBase.CanConnectToAbly()
                    });
                });
            }
        }

        private static bool ShouldUseFallbackHost(ErrorInfo error)
        {
            return error != null && error.StatusCode != null && FallbackReasons.Contains(error.StatusCode.Value) && AblyRealtime.CanConnectToAbly();
        }

        private void TransitionState(ConnectionState newState)
        {
            this.context.SetState(newState);
            _timer.Abort();
        }

        private bool ShouldSuspend()
        {
            return this.context.FirstConnectionAttempt != null &&
                this.context.FirstConnectionAttempt.Value
                .AddMilliseconds(ConnectionSuspendedState.SuspendTimeout) < DateTimeOffset.Now;
        }
    }
}
