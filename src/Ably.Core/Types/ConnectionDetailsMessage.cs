using MsgPack.Serialization;
using Newtonsoft.Json;

namespace Ably
{
    public class ConnectionDetailsMessage
    {
        public string clientId { get; set; }
        public string connectionKey { get; set; }
        public long maxMessageSize { get; set; }
        public long maxInboundRate { get; set; }
        public long maxFrameSize { get; set; }
        // TODO: add serverId property [don't know the type]
    }
}