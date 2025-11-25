using System;
using MessagePack;
using Newtonsoft.Json;

namespace IO.Ably
{
    /// <summary>
    /// provides details on the constraints or defaults for the connection such as max message size, client ID or connection state TTL.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class ConnectionDetails
    {
        /// <summary>
        /// Client id associated with the current connection.
        /// </summary>
        [Key("clientId")]
        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        /// <summary>
        /// Connection key.
        /// </summary>
        [Key("connectionKey")]
        [JsonProperty("connectionKey")]
        public string ConnectionKey { get; set; }

        /// <summary>
        /// Optional Connection state time to live.
        /// </summary>
        [Key("connectionStateTtl")]
        [JsonProperty("connectionStateTtl")]
        public TimeSpan? ConnectionStateTtl { get; set; }

        /// <summary>
        /// Max frame size.
        /// </summary>
        [Key("maxFrameSize")]
        [JsonProperty("maxFrameSize")]
        public long MaxFrameSize { get; set; }

        /// <summary>
        /// Max inbound rate.
        /// </summary>
        [Key("maxInboundRate")]
        [JsonProperty("maxInboundRate")]
        public long MaxInboundRate { get; set; }

        /// <summary>
        /// Max message size.
        /// </summary>
        [Key("maxMessageSize")]
        [JsonProperty("maxMessageSize")]
        public long MaxMessageSize { get; set; }

        /// <summary>
        /// Server id associated with the current connection.
        /// </summary>
        [Key("serverId")]
        [JsonProperty("serverId")]
        public string ServerId { get; set; }
    }
}
