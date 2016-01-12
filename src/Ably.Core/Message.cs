using System;
using System.Diagnostics;
using MsgPack.Serialization;
using Newtonsoft.Json;

namespace Ably
{
    /// <summary>
    /// A class representing an individual message to be sent or received via the Ably realtime service
    /// </summary>
    [DebuggerDisplay("{ToString()}")]
    public class Message : IEncodedMessage
    {
        internal const string IdPropertyName = "id";
        internal const string ClientIdPropertyName = "clientId";
        internal const string ConnectionIdPropertyName = "connection_id";
        internal const string NamePropertyName = "name";
        internal const string DataPropertyName = "data";
        internal const string EncodingPropertyName = "encoding";
        internal const string TimestampPropertyName = "timestamp";

        public Message()
        {

        }

        public Message(string name, object data)
        {
            this.name = name;
            this.data = data;
        }

        /// <summary>
        /// A globally unique message id
        /// </summary>
        [MessagePackMember(7, Name = IdPropertyName)]
        public string id { get; set; }

        /// <summary>
        /// The id of the publisher of this message
        /// </summary>
        [MessagePackMember(8, Name = ClientIdPropertyName)]
        public string clientId { get; set; }

        /// <summary>
        /// The connection id of the publisher of the message
        /// </summary>
        [MessagePackMember(9, Name = ConnectionIdPropertyName)]
        public string connection_id { get; set; }

        /// <summary>
        /// The event name, if avaible
        /// </summary>
        [MessagePackMember(10, Name = NamePropertyName)]
        public string name { get; set; }

        /// <summary>
        /// The message payload. Supported data types are objects, byte[] and strings
        /// </summary>
        [MessagePackMember(20, Name = DataPropertyName)]
        public object data { get; set; }

        /// <summary>
        /// The encoding for the message data. Encoding and decoding of messages is handled automatically by the client library.
        ///Therefore, the `encoding` attribute should always be nil unless an Ably library decoding error has occurred.
        /// </summary>
        [MessagePackMember(30, Name = EncodingPropertyName)]
        public string encoding { get; set; }

        /// <summary>
        /// Timestamp when the message was received by the Ably real-time service
        /// </summary>
        [MessagePackMember(40, Name = TimestampPropertyName)]
        public DateTimeOffset? timestamp { get; set; }

        public override string ToString()
        {
            var result = string.Format("Name: {0}, Data: {1}, Encoding: {2}, Timestamp: {3}", name, data, encoding, timestamp);
            if (id.IsNotEmpty())
                return "Id: " + id + ", " + result;
            return result;
        }
    }
}