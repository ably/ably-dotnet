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
        public DateTime Expires { get; set;}
        public DateTime IssuedAt { get; set; }
        public Capability Capability { get; set; }
        public string ClientId { get; set; }

        public static Token fromJSON(Newtonsoft.Json.Linq.JObject json)
        {
            var access_token = json["access_token"];
            if (access_token == null)
                return new Token();
            Token token = new Token();
            token.Id = (string)access_token["id"];
            token.Expires = ((long)access_token["expires"]).FromUnixTime();
            token.IssuedAt = ((long)access_token["issued_at"]).FromUnixTime();
            token.Capability = new Capability(access_token["capability"].ToString());
            //token.ClientId = json.Value<string>("client_id");
            return token;
        }
    }

    public sealed class TokenRequest
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
