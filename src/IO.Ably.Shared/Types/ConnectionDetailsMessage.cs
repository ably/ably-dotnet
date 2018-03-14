using System;

using Newtonsoft.Json;

namespace IO.Ably
{
    public class ConnectionDetails
    {
        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("connectionKey")]
        public string ConnectionKey { get; set; }

        [JsonProperty("connectionStateTtl")]
        public TimeSpan? ConnectionStateTtl { get; set; }

        [JsonProperty("maxFrameSize")]
        public long MaxFrameSize { get; set; }

        [JsonProperty("maxInboundRate")]
        public long MaxInboundRate { get; set; }

        [JsonProperty("maxMessageSize")]
        public long MaxMessageSize { get; set; }

        [JsonProperty("serverId")]
        public string ServerId { get; set; }
    }
}
