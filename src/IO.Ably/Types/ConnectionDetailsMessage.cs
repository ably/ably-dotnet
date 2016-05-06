using System;
using IO.Ably.CustomSerialisers;
using Newtonsoft.Json;

namespace IO.Ably
{
    public class ConnectionDetailsMessage
    {
        public string clientId { get; set; }
        public string connectionKey { get; set; }
        public TimeSpan? connectionStateTtl { get; set; }
        public long maxFrameSize { get; set; }
        public long maxInboundRate { get; set; }
        public long maxMessageSize { get; set; }
        public string serverId { get; set; }
    }
}