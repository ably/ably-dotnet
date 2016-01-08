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
            this.Action = action;
        }

        internal ProtocolMessage(MessageAction action, string channel)
        {
            this.Action = action;
            this.Channel = channel;
        }

        public MessageAction Action { get; set; }
        public MessageFlag Flags { get; set; }
        public int Count { get; set; }
        public ErrorInfo Error { get; set; }
        public string Id { get; set; }
        public string Channel { get; set; }
        public string ChannelSerial { get; set; }
        public string ConnectionId { get; set; }
        public string ConnectionKey { get; set; }
        public long? ConnectionSerial { get; set; }
        public long MsgSerial { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public Message[] Messages { get; set; }
        public PresenceMessage[] Presence { get; set; }

        public ConnectionDetailsMessage connectionDetails { get; set; }

        public override string ToString()
        {
            StringBuilder text = new StringBuilder();
            text.Append("{ ")
                .AppendFormat("action={0}", this.Action);
            if (this.Messages != null && this.Messages.Length > 0)
            {
                text.Append(", mesasages=[ ");
                foreach (Message message in this.Messages)
                {
                    text.AppendFormat("{{ name=\"{0}\"", message.Name);
                    if (message.Timestamp.HasValue && message.Timestamp.Value.Ticks > 0)
                    {
                        text.AppendFormat(", timestamp=\"{0}\"}}", message.Timestamp);
                    }
                    text.AppendFormat(", data={0}}}", message.Data);
                }
                text.Append(" ]");
            }
            text.Append(" }");
            return text.ToString();
        }
    }
}
