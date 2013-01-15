using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ably.Auth
{
    public sealed class Token
    {
        public String Id { get; set; }
        public long Expires { get; set;}
        public long IssuedAt { get; set; }
        public String Capability { get; set; }
        public String ClientId { get; set; }

        public static Token fromJSON(Newtonsoft.Json.Linq.JObject json)
        {
            Token token = new Token();
            token.Id = json.Value<string>("id");
            token.Expires = json.Value<long>("expires");
            token.IssuedAt = json.Value<long>("issued_at");
            token.Capability = json.Value<string>("capability");
            token.ClientId = json.Value<string>("client_id");
            return token;
        }
    }

    internal sealed class TokenRequest
    {
        [JsonProperty("id")]
        public String Id { get; set; }
        [JsonProperty("ttl")]
        public long Ttl { get; set; }
        [JsonProperty("capability")]
        public String Capability { get; set; }
        [JsonProperty("client_id")]
        public String ClientId { get; set; }
        [JsonProperty("timestamp")]
        public long TimeStamp { get; set; }
        [JsonProperty("nonce")]
        public String Nonce { get; set; }
        [JsonProperty("mac")]
        public String Mac { get; set; }

    }
}
