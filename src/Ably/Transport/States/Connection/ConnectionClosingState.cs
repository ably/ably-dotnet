using Ably.Types;
using System;

namespace Ably.Transport.States.Connection
{
    internal class ConnectionClosingState : ConnectionState
    {
        public ConnectionClosingState(IConnectionContext context) :
            base(context)
        { }

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
            throw new NotImplementedException();
        }

        public override void Close()
        {
            // do nothing
        }

        public override bool OnMessageReceived(ProtocolMessage message)
        {
            if (message.Action == ProtocolMessage.MessageAction.Closed)
            {
                this.context.SetState(new ConnectionClosedState(this.context));
                return true;
            }
            return false;
        }

        public override void OnTransportStateChanged(TransportStateInfo state)
        {
            if (state.State == TransportState.Closed)
            {
                this.context.SetState(new ConnectionClosedState(this.context));
            }
        }

        public override void OnAttachedToContext()
        {
            this.context.Transport.Close();
        }
    }
}
