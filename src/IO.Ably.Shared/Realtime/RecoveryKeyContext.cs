using Newtonsoft.Json;
using System.Collections.Generic;

namespace IO.Ably.Shared.Realtime
{
    internal class RecoveryKeyContext
    {
        [JsonProperty("connectionKey")]
        public string ConnectionKey { get; set; }

        [JsonProperty("msgSerial")]
        public long MsgSerial { get; set; }

        [JsonProperty("channelSerials")]
        public Dictionary<string, Dictionary<string, string>> ChannelSerials { get; set; }
    }
}
