using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace IO.Ably.Types
{
    public class ProtocolMessage
    {
        private string _connectionKey;

        public enum MessageAction : int
        {
            Heartbeat = 0,
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

        public class MessageFlags
        {
            public const int Presence = 1;
            public const int Backlog = 1 << 1;

            public static bool HasFlag(int? value, int flag)
            {
                if (value == null)
                {
                    return false;
                }

                return (value.Value & flag) != 0;
            }
        }

        public ProtocolMessage()
        {
            Messages = new Message[] { };
            Presence = new PresenceMessage[] { };
        }

        internal ProtocolMessage(MessageAction action)
            : this()
        {
            Action = action;
        }

        internal ProtocolMessage(MessageAction action, string channel)
            : this(action)
        {
            Channel = channel;
        }

        [JsonProperty("action")]
        public MessageAction Action { get; set; }

        [JsonProperty("flags")]
        public int? Flags { get; set; }

        [JsonIgnore]
        public bool HasPresenceFlag => MessageFlags.HasFlag(Flags, MessageFlags.Presence);

        [JsonIgnore]
        public bool HasBacklogFlag => MessageFlags.HasFlag(Flags, MessageFlags.Backlog);

        [JsonProperty("count")]
        public int? Count { get; set; }

        [JsonProperty("error")]
        public ErrorInfo Error { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("channelSerial")]
        public string ChannelSerial { get; set; }

        [JsonProperty("connectionId")]
        public string ConnectionId { get; set; }

        [JsonProperty("connectionKey")]
        public string ConnectionKey
        {
            get
            {
                if (ConnectionDetails != null && ConnectionDetails.ConnectionKey.IsNotEmpty())
                {
                    return ConnectionDetails.ConnectionKey;
                }

                return _connectionKey;
            }

            set { _connectionKey = value; }
        }

        [JsonProperty("connectionSerial")]
        public long? ConnectionSerial { get; set; }

        [JsonProperty("msgSerial")]
        public long MsgSerial { get; set; }

        [JsonProperty("timestamp")]
        public DateTimeOffset? Timestamp { get; set; }

        [JsonProperty("messages")]
        public Message[] Messages { get; set; }

        [JsonProperty("presence")]
        public PresenceMessage[] Presence { get; set; }

        [JsonProperty("connectionDetails")]
        public ConnectionDetails ConnectionDetails { get; set; }

        [JsonIgnore]
        internal bool AckRequired => Action == MessageAction.Message || Action == MessageAction.Presence;

        [OnSerializing]
        internal void OnSerializing(StreamingContext context)
        {
            if (Channel == string.Empty)
            {
                Channel = null;
            }

            // Filter out empty messages
            if (Messages != null)
            {
                Messages = Messages.Where(m => !m.IsEmpty).ToArray();
                if (Messages.Length == 0)
                {
                    Messages = null;
                }
            }

            if (Presence != null && Presence.Length == 0)
            {
                Presence = null;
            }
        }

        public override string ToString()
        {
            var text = new StringBuilder();
            text.Append("{ ")
                .AppendFormat("action={0}", Action);
            if (Messages != null && Messages.Length > 0)
            {
                text.Append(", mesasages=[ ");
                foreach (var message in Messages)
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
