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

        public enum MessageAction
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
            Sync,
            Auth
        }

        public enum Flag
        {
            HasPresence = 1 << 0,
            HasBacklog = 1 << 1,
            Resumed = 1 << 2,
            HasLocalPresence = 1 << 3,
            Transient = 1 << 4,
            Presence = 1 << 16,
            Publish = 1 << 17,
            Subscribe = 1 << 18,
            PresenceSubscribe = 1 << 19
        }

        public static bool HasFlag(int? value, Flag flag)
        {
            if (value == null)
            {
                return false;
            }

            return (value.Value & (int)flag) != 0;
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

        [JsonProperty("auth")]
        public AuthDetails Auth { get; set; }

        [JsonProperty("flags")]
        public int? Flags { get; set; }

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

        public bool HasFlag(Flag flag)
        {
            return HasFlag(Flags, flag);
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
