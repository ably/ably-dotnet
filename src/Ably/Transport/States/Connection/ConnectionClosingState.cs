using Ably.Types;
using System;

namespace Ably.Transport.States.Connection
{
    internal class ConnectionClosingState : ConnectionState
    {
        public ConnectionClosingState(IConnectionContext context) :
            this(context, new CountdownTimer())
        { }

        public ConnectionClosingState(IConnectionContext context, ICountdownTimer timer) :
            base(context)
        {
            _timer = timer;
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
            if (message.Action == ProtocolMessage.MessageAction.Closed)
            {
                _timer.Abort();
                this.context.SetState(new ConnectionClosedState(this.context));
                return true;
            }
            else if (message.Action == ProtocolMessage.MessageAction.Error)
            {
                _timer.Abort();
                this.context.SetState(new ConnectionFailedState(this.context, message.Error));
                return true;
            }
            return false;
        }

        public override void OnTransportStateChanged(TransportStateInfo state)
        {
            if (state.State == TransportState.Closed)
            {
                _timer.Abort();
                this.context.SetState(new ConnectionClosedState(this.context));
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
    }
}
