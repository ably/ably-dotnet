using System;
using System.Diagnostics;
using MsgPack.Serialization;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using Ably.Utils;

namespace Ably
{
    /// <summary>
    /// A class representing an individual message to be sent or received via the Ably realtime service
    /// </summary>
    [DebuggerDisplay( "{ToString()}" )]
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

        public Message( string name, object data )
        {
            this.name = name;
            this.data = data;
        }

        /// <summary>
        /// A globally unique message id
        /// </summary>
        [MessagePackMember( 7, Name = IdPropertyName )]
        public string id { get; set; }

        /// <summary>
        /// The id of the publisher of this message
        /// </summary>
        [MessagePackMember( 8, Name = ClientIdPropertyName )]
        public string clientId { get; set; }

        /// <summary>
        /// The connection id of the publisher of the message
        /// </summary>
        [MessagePackMember( 9, Name = ConnectionIdPropertyName )]
        public string connection_id { get; set; }

        /// <summary>The event name, if available</summary>
        [MessagePackMember( 10, Name = NamePropertyName )]
        public string name { get; set; }

        /// <summary>The message payload. Supported data types are objects, byte[] and strings.</summary>
        [MessagePackMember( 20, Name = DataPropertyName )]
        [JsonIgnore]
        public object data { get; set; }

        [JsonProperty( "data" )]
        public object data_raw { get; set; }

        [OnSerializing]
        internal void onSerializing( StreamingContext context )
        {
            if( this.data is byte[] )
            {
                this.data_raw = ( this.data as byte[] ).ToBase64();
                this.encoding = "base64";
            }
            else
                this.data_raw = this.data;
        }

        [OnSerialized]
        internal void onSerialized( StreamingContext context )
        {
            this.data_raw = null;
        }

        [OnDeserialized]
        internal void onDeserialized( StreamingContext context )
        {
            this.data = this.data_raw;
            this.data_raw = null;

            // Reduce precision of numbers
            if( this.data is long )
                this.data = (int)(long)this.data;
            if( this.data is double )
                this.data = (float)(double)this.data;

            if( this.encoding == "base64" && this.data is string )
                this.data = ( (string)this.data ).FromBase64();
        }

        /// <summary>
        /// The encoding for the message data. Encoding and decoding of messages is handled automatically by the client library.
        ///Therefore, the `encoding` attribute should always be nil unless an Ably library decoding error has occurred.
        /// </summary>
        [MessagePackMember( 30, Name = EncodingPropertyName )]
        public string encoding { get; set; }

        /// <summary>
        /// Timestamp when the message was received by the Ably real-time service
        /// </summary>
        [MessagePackMember( 40, Name = TimestampPropertyName )]
        public DateTimeOffset? timestamp { get; set; }

        public override string ToString()
        {
            var result = string.Format("Name: {0}, Data: {1}, Encoding: {2}, Timestamp: {3}", name, data, encoding, timestamp);
            if( id.IsNotEmpty() )
                return "Id: " + id + ", " + result;
            return result;
        }

        static readonly Message defaultInstance = new Message();

        /// <summary>True if all public properties have their default values.</summary>
        public bool isEmpty()
        {
            return ReflectionUtils.isPropsEqual( this, defaultInstance );
        }
    }
}