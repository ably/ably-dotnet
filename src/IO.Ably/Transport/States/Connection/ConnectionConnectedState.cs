using IO.Ably.Types;

namespace IO.Ably.Transport.States.Connection
{
    internal class ConnectionConnectedState : ConnectionState
    {
        public ConnectionConnectedState(IConnectionContext context, ConnectionInfo info) :
            base(context)
        {
            this.context.Connection.Id = info.ConnectionId;
            this.context.Connection.Key = info.ConnectionKey;
            this.context.Connection.Serial = info.ConnectionSerial;
        }

        public override Realtime.ConnectionState State => Realtime.ConnectionState.Connected;

        protected override bool CanQueueMessages => false;

        public override void Connect()
        {
            // do nothing
        }

        public override void Close()
        {
            context.SetState(new ConnectionClosingState(context));
        }

        public override bool OnMessageReceived(ProtocolMessage message)
        {
            switch (message.action)
            {
                case ProtocolMessage.MessageAction.Disconnected:
                {
                    context.SetState(new ConnectionDisconnectedState(context, message.error));
                    return true;
                }
                case ProtocolMessage.MessageAction.Error:
                {
                    context.SetState(new ConnectionFailedState(context, message.error));
                    return true;
                }
            }
            return false;
        }

        public override void OnTransportStateChanged(TransportStateInfo state)
        {
            if (state.State == TransportState.Closed)
            {
                context.SetState(new ConnectionDisconnectedState(context, state));
            }
        }

        public override void SendMessage(ProtocolMessage message)
        {
            context.Transport.Send(message);
        }

        public override void OnAttachedToContext()
        {
            context.ResetConnectionAttempts();

            if (context.QueuedMessages != null && context.QueuedMessages.Count > 0)
            {
                foreach (var message in context.QueuedMessages)
                {
                    SendMessage(message);
                }
                context.QueuedMessages.Clear();
            }
        }
    }
}