using System;
using System.Globalization;
using IO.Ably.Encryption;
using IO.Ably.Platform;
using IO.Ably.Transport;
using MsgPack.Serialization;
using Newtonsoft.Json;

namespace IO.Ably
{
    public class TokenRequest
    {
        public TokenRequest()
        {
            Nonce = Guid.NewGuid().ToString("N").ToLower();
        }

        internal TokenRequest Populate(TokenParams tokenParams, string keyName, string keyValue)
        {
            this.KeyName = keyName;
            Capability = (tokenParams.Capability ?? Ably.Capability.AllowAll).ToJson();
            ClientId = tokenParams.ClientId;
            var now = Config.Now();

            if (tokenParams.Nonce.IsNotEmpty())
                Nonce = tokenParams.Nonce;

            if (tokenParams.Ttl.HasValue)
                Ttl = tokenParams.Ttl.Value.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            else
                Ttl = Defaults.DefaultTokenTtl.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);

            if (tokenParams.Timestamp.HasValue)
                Timestamp = tokenParams.Timestamp.Value.ToUnixTimeInMilliseconds().ToString();
            else
                Timestamp = now.ToUnixTimeInMilliseconds().ToString();

            CalculateMac(keyValue);

            return this;
        }

        [JsonProperty("keyName")]
        [MessagePackMember(10, Name = "keyName")]
        public string KeyName { get; set; }

        [JsonProperty("ttl")]
        [MessagePackMember(20, Name = "ttl")]
        public string Ttl { get; set; }

        [JsonProperty("capability", NullValueHandling = NullValueHandling.Ignore)]
        [MessagePackMember(30, Name = "capability")]
        public string Capability { get; set; }

        [JsonProperty("clientId", NullValueHandling = NullValueHandling.Ignore)]
        [MessagePackMember(40, Name = "clientId")]
        public string ClientId { get; set; }

        [JsonProperty("timestamp")]
        [MessagePackMember(50, Name = "timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("nonce")]
        [MessagePackMember(60, Name = "nonce")]
        public string Nonce { get; set; }

        [JsonProperty("mac")]
        [MessagePackMember(70, Name = "mac")]
        public string Mac { get; set; }

        public void CalculateMac(string key)
        {
            var values = new[]
                {
                    KeyName,
                    Ttl,
                    Capability,
                    ClientId,
                    Timestamp,
                    Nonce
                };

            var signText = string.Join("\n", values) + "\n";
            Mac = Crypto.ComputeHMacSha256(signText, key);
        }

        public override int GetHashCode()
        {
            return KeyName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as TokenRequest;
            if (other == null)
                return false;
            return KeyName == other.KeyName
                && Ttl == other.Ttl
                && Capability == other.Capability
                && ClientId == other.ClientId
                && Timestamp == other.Timestamp
                && Nonce == other.Nonce
                && Mac == other.Mac;
        }
    }
}
