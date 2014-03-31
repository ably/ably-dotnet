using Newtonsoft.Json;

namespace Ably
{
    public class MessagePayload
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("data")]
        public object Data { get; set; }
        [JsonProperty("encoding", NullValueHandling = NullValueHandling.Ignore)]
        public string Encoding { get; set; }
        [JsonProperty("timestamp")]
        public long? Timestamp { get; set; }
    }
}