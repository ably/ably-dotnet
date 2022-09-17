using System;
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
        public Dictionary<string, string> ChannelSerials { get; set; }

        public string Encode()
        {
            return JsonHelper.Serialize(this);
        }

        public static RecoveryKeyContext Decode(string recover)
        {
            try
            {
                return JsonHelper.Deserialize<RecoveryKeyContext>(recover);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
