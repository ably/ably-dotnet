using MsgPack.Serialization;
using Newtonsoft.Json;

namespace Ably
{
    public class ConnectionDetailsMessage
    {
        [JsonProperty, MessagePackMember( 0 )]
        public string clientId { get; set; }
        [JsonProperty, MessagePackMember( 1 )]
        public string connectionKey { get; set; }
        [JsonProperty, MessagePackMember( 2 )]
        public long maxMessageSize { get; set; }
        [JsonProperty, MessagePackMember( 3 )]
        public long maxInboundRate { get; set; }
        [JsonProperty, MessagePackMember( 4 )]
        public long maxFrameSize { get; set; }

        // TODO: add serverId property [don't know the type]
    }
}