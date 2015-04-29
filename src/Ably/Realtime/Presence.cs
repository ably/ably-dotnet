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
            this.channel = channel;
        }

        private IConnectionManager connection;
        private string channel;

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

        public void Subscribe(Action<PresenceMessage[]> callback)
        {

        }

        public void Unsubscribe(Action<PresenceMessage[]> callback)
        {

        }

        private void UpdatePresence(PresenceMessage msg, Action<bool, ErrorInfo> callback)
        {
            ProtocolMessage message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, this.channel);
            message.Presence = new PresenceMessage[] { msg };
            this.connection.Send(message, callback);
        }
    }
}
