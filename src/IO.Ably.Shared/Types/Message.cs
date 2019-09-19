using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using IO.Ably.MessageEncoders;
using IO.Ably.Utils;
using Newtonsoft.Json;

namespace IO.Ably
{
    /// <summary>A class representing an individual message to be sent or received via the Ably realtime service</summary>
    [DebuggerDisplay("{ToString()}")]
    public class Message : IMessage
    {
        private static readonly Message DefaultInstance = new Message();

        public Message()
        {
        }

        public Message(string name = null, object data = null, string clientId = null)
        {
            Name = name;
            Data = data;
            if (clientId.IsNotEmpty())
            {
                ClientId = clientId;
            }
        }

        /// <summary>A globally unique message id</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>The id of the publisher of this message</summary>
        ///
        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        /// <summary>The connection id of the publisher of the message</summary>
        [JsonProperty("connectionId")]
        public string ConnectionId { get; set; }

        /// <summary>The event name, if available</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>Timestamp when the message was received by the Ably real-time service</summary>
        [JsonProperty("timestamp")]
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>The message payload. Supported data types are objects, byte[] and strings.</summary>
        [JsonProperty("data")]
        public object Data { get; set; }

        /// <summary>
        ///     The encoding for the message data. Encoding and decoding of messages is handled automatically by the client
        ///     library.
        ///     Therefore, the `encoding` attribute should always be nil unless an Ably library decoding error has occurred.
        /// </summary>
        [JsonProperty("encoding")]
        public string Encoding { get; set; }

        public override string ToString()
        {
            var result = $"Name: {Name}, Data: {Data}, Encoding: {Encoding}, Timestamp: {Timestamp}";
            if (Id.IsNotEmpty())
            {
                return "Id: " + Id + ", " + result;
            }

            return result;
        }

        [JsonIgnore]
        public bool IsEmpty => Equals(this, DefaultInstance);

        protected bool Equals(Message other)
        {
            return string.Equals(Id, other.Id) && string.Equals(ClientId, other.ClientId) && string.Equals(ConnectionId, other.ConnectionId) && string.Equals(Name, other.Name) && Timestamp.Equals(other.Timestamp) && Equals(Data, other.Data) && string.Equals(Encoding, other.Encoding);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Message)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id != null ? Id.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (ClientId != null ? ClientId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ConnectionId != null ? ConnectionId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Timestamp.GetHashCode();
                hashCode = (hashCode * 397) ^ (Data != null ? Data.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Encoding != null ? Encoding.GetHashCode() : 0);
                return hashCode;
            }
        }


        public static Message FromEncoded(Message encoded, ChannelOptions options = null)
        {
            return MessageHandler.FromEncoded(encoded, options);
        }

        public static Message[] FromEncodedArray(Message[] encoded, ChannelOptions options = null)
        {
            return MessageHandler.FromEncodedArray(encoded, options);
        }

        public static Message FromEncoded(string messageJson, ChannelOptions options = null)
        {
            try
            {
                var message = JsonHelper.Deserialize<Message>(messageJson);
                return FromEncoded(message, options);
            }
            catch (Exception e)
            {
                DefaultLogger.Error($"Error decoding message: {messageJson}", e);
                throw new AblyException("Error decoding message. Error: " + e.Message, 50000);
            }
        }

        public static Message[] FromEncodedArray(string messagesJson, ChannelOptions options = null)
        {
            try
            {
                var messages = JsonHelper.Deserialize<List<Message>>(messagesJson).ToArray();
                return FromEncodedArray(messages, options);

            }
            catch (Exception e)
            {
                DefaultLogger.Error($"Error decoding message: {messagesJson}", e);
                throw new AblyException("Error decoding messages. Error: " + e.Message, 50000);
            }
        }
    }
}
