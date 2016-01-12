using System;
using System.Text;

namespace Ably.Types
{
    public class ProtocolMessage
    {
        public enum MessageAction : int
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

        [Flags]
        public enum MessageFlag : byte
        {
            Has_Presence,
            Has_Backlog
        }

        internal const string ActionPropertyName = "action";
        internal const string FlagsPropertyName = "flags";
        internal const string CountPropertyName = "count";
        internal const string ErrorPropertyName = "error";
        internal const string IdPropertyName = "id";
        internal const string ChannelPropertyName = "channel";
        internal const string ChannelSerialPropertyName = "channelSerial";
        internal const string ConnectionIdPropertyName = "connectionId";
        internal const string ConnectionKeyPropertyName = "connectionKey";
        internal const string ConnectionSerialPropertyName = "connectionSerial";
        internal const string MsgSerialPropertyName = "msgSerial";
        internal const string TimestampPropertyName = "timestamp";
        internal const string MessagesPropertyName = "messages";
        internal const string PresencePropertyName = "presence";

        internal ProtocolMessage()
        {
        }

        internal ProtocolMessage(MessageAction action)
        {
            this.action = action;
        }

        internal ProtocolMessage(MessageAction action, string channel)
        {
            this.action = action;
            this.channel = channel;
        }

        public MessageAction action { get; set; }
        public MessageFlag flags { get; set; }
        public int count { get; set; }
        public ErrorInfo error { get; set; }
        public string id { get; set; }
        public string channel { get; set; }
        public string channelSerial { get; set; }
        public string connectionId { get; set; }
        public string connectionKey { get; set; }
        public long? connectionSerial { get; set; }
        public long msgSerial { get; set; }
        public DateTimeOffset? timestamp { get; set; }
        public Message[] messages { get; set; }
        public PresenceMessage[] presence { get; set; }
        public ConnectionDetailsMessage connectionDetails { get; set; }

        public override string ToString()
        {
            StringBuilder text = new StringBuilder();
            text.Append("{ ")
                .AppendFormat("action={0}", this.action);
            if (this.messages != null && this.messages.Length > 0)
            {
                text.Append(", mesasages=[ ");
                foreach (Message message in this.messages)
                {
                    text.AppendFormat("{{ name=\"{0}\"", message.name);
                    if (message.timestamp.HasValue && message.timestamp.Value.Ticks > 0)
                    {
                        text.AppendFormat(", timestamp=\"{0}\"}}", message.timestamp);
                    }
                    text.AppendFormat(", data={0}}}", message.data);
                }
                text.Append(" ]");
            }
            text.Append(" }");
            return text.ToString();
        }
    }
}
