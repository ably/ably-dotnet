using Ably.Types;
using System;

namespace Ably.Transport.States.Connection
{
    internal class ConnectionConnectingState : ConnectionState
    {
        public ConnectionConnectingState(IConnectionContext context) :
            this(context, new CountdownTimer())
        { }

        public ConnectionConnectingState(IConnectionContext context, ICountdownTimer timer) :
            base(context)
        {
            _timer = timer;
        }

        private ICountdownTimer _timer;
        private const int ConnectTimeout = 15 * 1000;

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
                            ConnectionInfo info = new ConnectionInfo(message.ConnectionId, message.ConnectionSerial, message.ConnectionKey);
                            this.TransitionState(new ConnectionConnectedState(this.context, info));
                        }
                        return true;
                    }
                case ProtocolMessage.MessageAction.Disconnected:
                    {
                        ConnectionState nextState;
                        if (this.ShouldSuspend())
                        {
                            nextState = new ConnectionSuspendedState(this.context);
                        }
                        else
                        {
                            nextState = new ConnectionDisconnectedState(this.context, message.Error);
                        }
                        this.TransitionState(nextState);
                        return true;
                    }
                case ProtocolMessage.MessageAction.Error:
                    {
                        this.TransitionState(new ConnectionFailedState(this.context, message.Error));
                        return true;
                    }
            }
            return false;
        }

        public override void OnTransportStateChanged(TransportStateInfo state)
        {
            if (state.State == TransportState.Closed)
            {
                ConnectionState nextState;
                if (this.ShouldSuspend())
                {
                    nextState = new ConnectionSuspendedState(this.context);
                }
                else
                {
                    nextState = new ConnectionDisconnectedState(this.context, state);
                }
                this.TransitionState(nextState);
            }
        }

        public override void OnAttachedToContext()
        {
            context.AttemptConnection();

            if (context.Transport == null)
            {
                context.CreateTransport();
            }

            if (context.Transport.State != TransportState.Connected)
            {
                this.context.Transport.Connect();
                _timer.Start(ConnectTimeout, () => this.context.SetState(new ConnectionDisconnectedState(this.context)));
            }
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
