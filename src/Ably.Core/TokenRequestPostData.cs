using System;
using Newtonsoft.Json;

namespace Ably
{
    public class TokenRequestPostData
    {
        public TokenRequestPostData()
        {
            nonce = Guid.NewGuid().ToString("N").ToLower();
        }

        public string keyName { get; set; }
        public string ttl { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string capability { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string clientId { get; set; }
        public string timestamp { get; set; }
        public string nonce { get; set; }
        public string mac { get; set; }

        public void CalculateMac(string key)
        {
            var values = new[]
                {
                    keyName,
                    ttl,
                    capability,
                    clientId,
                    timestamp,
                    nonce
                };

            var signText = string.Join("\n", values) + "\n";
            mac = Encryption.Crypto.ComputeHMacSha256( signText, key );
        }

        public override int GetHashCode()
        {
            return keyName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as TokenRequestPostData;
            if (other == null)
                return false;
            return keyName == other.keyName
                && ttl == other.ttl
                && capability == other.capability
                && clientId == other.clientId
                && timestamp == other.timestamp
                && nonce == other.nonce
                && mac == other.mac;
        }
    }
}
