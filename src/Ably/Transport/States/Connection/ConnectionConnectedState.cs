using Ably.Types;

namespace Ably.Transport.States.Connection
{
    internal class ConnectionConnectedState : ConnectionState
    {
        public ConnectionConnectedState(IConnectionContext context, ConnectionInfo info) :
            base(context)
        {
            this.ConnectionInfo = info;
        }

        public override Realtime.ConnectionState State
        {
            get
            {
                return Realtime.ConnectionState.Connected;
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
            // Notify ably service
            this.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Close));
            this.context.SetState(new ConnectionClosingState(this.context));
        }

        public override bool OnMessageReceived(ProtocolMessage message)
        {
            switch (message.Action)
            {
                case ProtocolMessage.MessageAction.Disconnected:
                    {
                        this.context.SetState(new ConnectionDisconnectedState(this.context, message.Error));
                        return true;
                    }
                case ProtocolMessage.MessageAction.Error:
                    {
                        this.context.SetState(new ConnectionFailedState(this.context, message.Error));
                        return true;
                    }
            }
            return false;
        }

        public override void OnTransportStateChanged(TransportStateInfo state)
        {
            if (state.State == TransportState.Closed)
            {
                this.context.SetState(new ConnectionDisconnectedState(this.context, state));
            }
        }

        public override void SendMessage(ProtocolMessage message)
        {
            context.Transport.Send(message);
        }

        public override void OnAttachedToContext()
        {
            if (this.context.QueuedMessages != null && this.context.QueuedMessages.Count > 0)
            {
                foreach (ProtocolMessage message in this.context.QueuedMessages)
                {
                    this.SendMessage(message);
                }
                this.context.QueuedMessages.Clear();
            }
        }
    }
}
