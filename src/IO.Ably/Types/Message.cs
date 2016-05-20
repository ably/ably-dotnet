using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using IO.Ably.Utils;
using Newtonsoft.Json;

namespace IO.Ably
{
    /// <summary>A class representing an individual message to be sent or received via the Ably realtime service</summary>
    [DebuggerDisplay("{ToString()}")]
    public class Message : IMessage
    {
        private static readonly Message defaultInstance = new Message();

        public Message()
        {
        }

        public Message(string name, object data)
        {
            this.name = name;
            this.data = data;
        }

        /// <summary>A globally unique message id</summary>
        public string id { get; set; }

        /// <summary>The id of the publisher of this message</summary>
        public string clientId { get; set; }

        /// <summary>The connection id of the publisher of the message</summary>
        public string connectionId { get; set; }

        /// <summary>The event name, if available</summary>
        public string name { get; set; }

        [JsonProperty("data")]
        public object data_raw { get; set; }

        /// <summary>Timestamp when the message was received by the Ably real-time service</summary>
        public DateTimeOffset? timestamp { get; set; }

        /// <summary>The message payload. Supported data types are objects, byte[] and strings.</summary>
        [JsonIgnore]
        public object data { get; set; }

        /// <summary>
        ///     The encoding for the message data. Encoding and decoding of messages is handled automatically by the client
        ///     library.
        ///     Therefore, the `encoding` attribute should always be nil unless an Ably library decoding error has occurred.
        /// </summary>
        public string encoding { get; set; }

        [OnSerializing]
        internal void OnSerializing(StreamingContext context)
        {
            if (data is byte[])
            {
                data_raw = (data as byte[]).ToBase64();
                encoding = "base64";
            }
            else
                data_raw = data;
        }

        [OnSerialized]
        internal void OnSerialized(StreamingContext context)
        {
            data_raw = null;
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            data = data_raw;
            data_raw = null;
        }

        public override string ToString()
        {
            var result = string.Format("Name: {0}, Data: {1}, Encoding: {2}, Timestamp: {3}", name, data, encoding,
                timestamp);
            if (id.IsNotEmpty())
                return "Id: " + id + ", " + result;
            return result;
        }

        [JsonIgnore]
        public bool IsEmpty => Equals(this, defaultInstance);

        protected bool Equals(Message other)
        {
            return string.Equals(id, other.id) && string.Equals(clientId, other.clientId) && string.Equals(connectionId, other.connectionId) && string.Equals(name, other.name) && timestamp.Equals(other.timestamp) && Equals(data, other.data) && string.Equals(encoding, other.encoding);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Message) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (id != null ? id.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (clientId != null ? clientId.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (connectionId != null ? connectionId.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (name != null ? name.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ timestamp.GetHashCode();
                hashCode = (hashCode*397) ^ (data != null ? data.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (encoding != null ? encoding.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}