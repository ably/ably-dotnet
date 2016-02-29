using Newtonsoft.Json;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace IO.Ably.Types
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

        public ProtocolMessage()
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

        // http://stackoverflow.com/a/24224465/126995
        [JsonIgnore]
        public MessageFlag flags { get; set; }
        [JsonProperty( "flags" )]
        public MessageFlag flag_setOnly { set { this.flags = value; } }

        [JsonIgnore]
        public int count { get; set; }
        [JsonProperty( "count" )]
        public int count_setOnly { set { this.count = value; } }

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

        public bool ShouldSerializemessages()
        {
            if( null == messages )
                return false;
            return messages.Any( m => !m.isEmpty() );
        }

        [OnSerializing]
        internal void onSerializing( StreamingContext context )
        {
            if( "" == channel )
                channel = null;

            // Filter out empty messages
            if( null == this.messages )
                return;
            this.messages = this.messages.Where( m => !m.isEmpty() ).ToArray();
            if( this.messages.Length <= 0 )
                this.messages = null;
        }

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
