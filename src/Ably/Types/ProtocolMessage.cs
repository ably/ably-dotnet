using System;

namespace Ably.Types
{
    public class ProtocolMessage
    {
        public enum Action
        {
            Heartbeat,
            Ack,
            Nack,
            Connect,
            Connected,
            Disconnect,
            Disconnected,
            Close,
            Closed,
            Error,
            Attach,
            Attached,
            Detach,
            Detached,
            Presence,
            Message,
            Sync
        }

        public enum Flag
        {
            Has_Presence,
            Has_Backlog
        }

        internal ProtocolMessage()
        {
        }

        internal ProtocolMessage(Action action)
        {
            this.action = action;
        }

        internal ProtocolMessage(Action action, string channel)
        {
            this.action = action;
        }

        private Action action;
        private string channel;

        public string ToJSON()
        {
            return "";
        }

        public byte[] ToMsgpack()
        {
            return new byte[0];
        }
    }
}
