using Ably.Types;
using System;

namespace Ably.Transport.States.Connection
{
    internal class ConnectionClosingState : ConnectionState
    {
        public ConnectionClosingState(IConnectionContext context) :
            this(context, null, new CountdownTimer())
        { }

        public ConnectionClosingState(IConnectionContext context, ErrorInfo error) :
            this(context, error, new CountdownTimer())
        { }

        public ConnectionClosingState(IConnectionContext context, ErrorInfo error, ICountdownTimer timer) :
            base(context)
        {
            _timer = timer;
            this.Error = error ?? ErrorInfo.ReasonClosed;
        }

        private const int CloseTimeout = 1000;
        private ICountdownTimer _timer;

        public override Realtime.ConnectionState State
        {
            get
            {
                return Realtime.ConnectionState.Closing;
            }
        }

        protected override bool CanQueueMessages
        {
            get
            {
                return false;
            }
        }

        public override void Connect()
        {
            // do nothing
        }

        public override void Close()
        {
            // do nothing
        }

        public override bool OnMessageReceived(ProtocolMessage message)
        {
            switch (message.Action)
            {
                case ProtocolMessage.MessageAction.Closed:
                    {
                        this.TransitionState(new ConnectionClosedState(this.context));
                        return true;
                    }
                case ProtocolMessage.MessageAction.Disconnected:
                    {
                        this.TransitionState(new ConnectionDisconnectedState(this.context, message.Error));
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
                this.TransitionState(new ConnectionClosedState(this.context));
            }
        }

        public override void OnAttachedToContext()
        {
            if (this.context.Transport.State == TransportState.Connected)
            {
                this.context.Transport.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Close));
                _timer.Start(CloseTimeout, () => this.context.SetState(new ConnectionClosedState(this.context)));
            }
            else
            {
                this.context.SetState(new ConnectionClosedState(this.context));
            }
        }

        private void TransitionState(ConnectionState newState)
        {
            this.context.SetState(newState);
            _timer.Abort();
        }
    }
}
