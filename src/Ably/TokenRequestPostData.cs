using System;
using Ably.Auth;
using MsgPack.Serialization;
using Newtonsoft.Json;

namespace Ably
{
    public class TokenResponse
    {
        [JsonProperty("access_token")]
        [MessagePackMember(1, Name = "access_token")]
        public Token AccessToken { get; set; }
    }

    public class TokenRequestPostData
    {
        public TokenRequestPostData()
        {
            nonce = Guid.NewGuid().ToString("N").ToLower();
        }

        public string id { get; set; }
        public string ttl { get; set; }
        public string capability { get; set; }
        public string clientId { get; set; }
        public string timestamp { get; set; }
        public string nonce { get; set; }
        public string mac { get; set; }

        public void CalculateMac(string key)
        {
            var values = new[] 
                { 
                    id, 
                    ttl,
                    capability, 
                    clientId, 
                    timestamp,
                    nonce
                };

            var signText = string.Join("\n", values) + "\n";

            mac = signText.ComputeHMacSha256(key);
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as TokenRequestPostData;
            if (other == null)
                return false;
            return id == other.id
                && ttl == other.ttl
                && capability == other.capability
                && clientId == other.clientId
                && timestamp == other.timestamp
                && nonce == other.nonce
                && mac == other.mac;
        }
    }
}
