using Ably.Types;
using System;

namespace Ably.Transport.States.Connection
{
    internal class ConnectionConnectingState : ConnectionState
    {
        public ConnectionConnectingState(IConnectionContext context) :
            base(context)
        { }

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
            this.context.SetState(new ConnectionClosingState(this.context));
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
                            this.context.SetState(new ConnectionConnectedState(this.context, info));
                        }
                        return true;
                    }
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
            if (state.State == TransportState.Connected)
            {
                this.context.Transport.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Connect));
            }
            else if (state.State == TransportState.Closed)
            {
                this.context.SetState(new ConnectionDisconnectedState(this.context, state));
            }
        }

        public override void OnAttachedToContext()
        {
            if (context.Transport == null)
            {
                context.CreateTransport();
            }

            if (context.Transport.State == TransportState.Connected)
            {
                this.context.Transport.Send(new ProtocolMessage(ProtocolMessage.MessageAction.Connect));
            }
            else
            {
                this.context.Transport.Connect();
            }
        }
    }
}
