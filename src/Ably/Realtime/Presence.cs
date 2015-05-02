using Ably.Transport;
using Ably.Types;
using System;
using System.Collections.Generic;

namespace Ably.Realtime
{
    public class Presence
    {
        public Presence(IConnectionManager connection, string channel)
        {
            this.connection = connection;
            this.connection.MessageReceived += OnConnectionMessageReceived;
            this.channel = channel;
        }

        private IConnectionManager connection;
        private string channel;

        public event Action<PresenceMessage[]> MessageReceived;

        public void Enter(object clientData, Action<bool, ErrorInfo> callback)
        {

        }

        public void EnterClient(string clientId, object clientData, Action<bool, ErrorInfo> callback)
        {
            this.UpdatePresence(new PresenceMessage(PresenceMessage.ActionType.Enter, clientId, clientData), callback);
        }

        public void Update(object clientData, Action<bool, ErrorInfo> callback)
        {

        }

        public void UpdateClient(string clientId, object clientData, Action<bool, ErrorInfo> callback)
        {
            this.UpdatePresence(new PresenceMessage(PresenceMessage.ActionType.Update, clientId, clientData), callback);
        }

        public void Leave(Action<bool, ErrorInfo> callback)
        {

        }

        public void LeaveClient(string clientId, Action<bool, ErrorInfo> callback)
        {
            this.UpdatePresence(new PresenceMessage(PresenceMessage.ActionType.Leave, clientId), callback);
        }

        private void UpdatePresence(PresenceMessage msg, Action<bool, ErrorInfo> callback)
        {
            ProtocolMessage message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, this.channel);
            message.Presence = new PresenceMessage[] { msg };
            this.connection.Send(message, callback);
        }

        private void OnConnectionMessageReceived(ProtocolMessage message)
        {
            switch (message.Action)
            {
                case ProtocolMessage.MessageAction.Presence:
                    this.OnPresence(message, null);
                    break;
                case ProtocolMessage.MessageAction.Sync:
                    this.OnSync(message);
                    break;
            }
        }

        private void OnPresence(ProtocolMessage message, string channelSerial)
        {
            // TODO: Implement
            this.Publish(message.Presence);
        }

        private void OnSync(ProtocolMessage message)
        {
            // TODO: Implement Channel.OnSync
            this.OnPresence(message, "");
        }

        private void Publish(params PresenceMessage[] messages)
        {
            if (this.MessageReceived != null)
            {
                this.MessageReceived(messages);
            }
        }
    }
}
