using System;

using IO.Ably;
using IO.Ably.Encryption;

using Newtonsoft.Json;

namespace IO.Ably
{
    public class TokenRequest
    {
        internal Func<DateTimeOffset> Now { get; set; }

        private DateTimeOffset? _timestamp;

        public TokenRequest()
            : this(Defaults.NowFunc())
        { }

        internal TokenRequest(Func<DateTimeOffset> nowFunc)
        {
            Now = nowFunc;
            Nonce = Guid.NewGuid().ToString("N").ToLower();
        }

        /// <summary>
        /// The Id against which the request is made
        /// </summary>
        [JsonProperty("keyName")]
        public string KeyName { get; set; }

        /// <summary>
        /// Requested time to live for the token. If the token request
        /// is successful, the TTL of the returned token will be less
        /// than or equal to this value depending on application settings
        /// and the attributes of the issuing key
        /// </summary>
        [JsonProperty("ttl")]
        public TimeSpan? Ttl { get; set; }

        /// <summary>
        /// <see cref="Capability"/> of the token. If the token request is successful,
        /// the capability of the returned token will be the intersection of
        /// this capability with the capability of the issuing key.
        /// </summary>
        [JsonProperty("capability", NullValueHandling = NullValueHandling.Ignore)]
        public Capability Capability { get; set; }

        /// <summary>
        /// ClientId to associate with the current token. The generated token may be to authenticate as this tokenId.
        /// </summary>
        [JsonProperty("clientId", NullValueHandling = NullValueHandling.Ignore)]
        public string ClientId { get; set; }

        /// <summary>
        /// The timestamp  of this request. If not supplied the timestamp is automatically set to the current UTC time
        /// Timestamps, in conjunction with the nonce, are used to prevent
        /// token requests from being replayed.
        /// </summary>
        [JsonProperty("timestamp")]
        public DateTimeOffset? Timestamp
        {
            get { return _timestamp; }

            set
            {
                if (value != DateTimeOffset.MinValue)
                {
                    _timestamp = value;
                }
            }
        }

        /// <summary>
        /// An opaque nonce string of at least 16 characters to ensure
        /// uniqueness of this request. Any subsequent request using the
        /// same nonce will be rejected.
        /// </summary>
        ///
        [JsonProperty("nonce")]
        public string Nonce { get; set; }

        [JsonProperty("mac")]
        public string Mac { get; set; }

        internal TokenRequest Populate(TokenParams tokenParams, string keyName, string keyValue)
        {
            KeyName = keyName;
            Capability = tokenParams.Capability ?? Capability.AllowAll;
            ClientId = tokenParams.ClientId;
            var now = Now();

            if (tokenParams.Nonce.IsNotEmpty())
            {
                Nonce = tokenParams.Nonce;
            }

            Ttl = tokenParams.Ttl ?? Defaults.DefaultTokenTtl;

            Timestamp = tokenParams.Timestamp ?? now;

            CalculateMac(keyValue);

            return this;
        }

        private void CalculateMac(string key)
        {
            var values = new[]
                {
                    KeyName,
                    Ttl?.TotalMilliseconds.ToString(),
                    Capability?.ToJson(),
                    ClientId,
                    Timestamp?.ToUnixTimeInMilliseconds().ToString(),
                    Nonce
                };

            var signText = string.Join("\n", values) + "\n";
            Mac = Crypto.ComputeHMacSha256(signText, key);
        }
    }
}
