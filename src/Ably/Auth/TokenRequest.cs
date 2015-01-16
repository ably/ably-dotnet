using System;
using Newtonsoft.Json;

namespace Ably.Auth
{
    public sealed class TokenRequest
    {
        [JsonProperty("id")]
        public String Id { get; set; }
        [JsonProperty("ttl")]
        public long Ttl { get; set; }
        [JsonProperty("capability")]
        public String Capability { get; set; }
        [JsonProperty("clientId")]
        public String ClientId { get; set; }
        [JsonProperty("timestamp")]
        public long TimeStamp { get; set; }
        [JsonProperty("nonce")]
        public String Nonce { get; set; }
        [JsonProperty("mac")]
        public String Mac { get; set; }
    }
}