using System;
using MessagePack;
using Newtonsoft.Json;

namespace IO.Ably
{
    /// <summary>
    /// provides details on the constraints or defaults for the connection such as max message size, client ID or connection state TTL.
    /// </summary>
    [MessagePackObject]
    public class ConnectionDetails
    {
        /// <summary>
        /// Client id associated with the current connection.
        /// </summary>
        [Key(0)]
        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        /// <summary>
        /// Connection key.
        /// </summary>
        [Key(1)]
        [JsonProperty("connectionKey")]
        public string ConnectionKey { get; set; }

        /// <summary>
        /// Optional Connection state time to live.
        /// </summary>
        [Key(2)]
        [JsonProperty("connectionStateTtl")]
        public TimeSpan? ConnectionStateTtl { get; set; }

        /// <summary>
        /// Max frame size.
        /// </summary>
        [Key(3)]
        [JsonProperty("maxFrameSize")]
        public long MaxFrameSize { get; set; }

        /// <summary>
        /// Max inbound rate.
        /// </summary>
        [Key(4)]
        [JsonProperty("maxInboundRate")]
        public long MaxInboundRate { get; set; }

        /// <summary>
        /// Max message size.
        /// </summary>
        [Key(5)]
        [JsonProperty("maxMessageSize")]
        public long MaxMessageSize { get; set; }

        /// <summary>
        /// Server id associated with the current connection.
        /// </summary>
        [Key(6)]
        [JsonProperty("serverId")]
        public string ServerId { get; set; }
    }
}
