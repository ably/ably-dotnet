using System;
using System.Collections.Generic;
using System.Linq;

namespace IO.Ably
{
    public class TokenParams
    {
        /// <summary>
        /// Requested time to live for the token. If the token request
		/// is successful, the TTL of the returned token will be less
		/// than or equal to this value depending on application settings
		/// and the attributes of the issuing key
        /// </summary>
        public TimeSpan? Ttl { get; set; }

        /// <summary>
		/// <see cref="Capability"/> of the token. If the token request is successful,
		/// the capability of the returned token will be the intersection of
		/// this capability with the capability of the issuing key.
        /// </summary>
        public Capability Capability { get; set; }

        /// <summary>
        /// ClientId to associate with the current token. The generated token may be to authenticate as this tokenId.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// The timestamp  of this request. If not supplied the timestamp is automatically set to the current UTC time
		/// Timestamps, in conjunction with the nonce, are used to prevent
		/// token requests from being replayed.
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>
		/// An opaque nonce string of at least 16 characters to ensure
		/// uniqueness of this request. Any subsequent request using the
		/// same nonce will be rejected.
        /// </summary>
        public string Nonce { get; set; }

        public TokenParams Merge(TokenParams otherParams)
        {
            if (otherParams == null)
                return this;

            var result = new TokenParams();
            result.ClientId = ClientId.IsNotEmpty() ? ClientId : otherParams.ClientId;
            result.Capability = Capability ?? otherParams.Capability;
            result.Ttl = Ttl ?? otherParams.Ttl;
            result.Timestamp = Timestamp ?? otherParams.Timestamp;
            result.Nonce = Nonce ?? otherParams.Nonce;
            return result;
        }

        public TokenParams Clone()
        {
            var result = new TokenParams();
            result.ClientId = ClientId;
            if(Capability != null)
                result.Capability = new Capability(Capability.ToJson());
            result.Nonce = Nonce;
            result.Ttl = Ttl;
            result.Timestamp = Timestamp;
            return result;
        }

        public static TokenParams WithDefaultsApplied()
        {
            var tokenParams = new TokenParams
            {
                Capability = Defaults.DefaultTokenCapability,
                Ttl = Defaults.DefaultTokenTtl
            };
            return tokenParams;
        }

        public Dictionary<string, string> ToRequestParams(Dictionary<string, string> mergeWith = null)
        {
            var dictionary = new Dictionary<string,string>();
            if(Ttl.HasValue)
                dictionary.Add("ttl", Ttl.Value.TotalMilliseconds.ToString());
            if(ClientId.IsNotEmpty())
                dictionary.Add("clientId", ClientId);
            if(Nonce.IsNotEmpty())
                dictionary.Add("nonce", Nonce);
            if (Capability != null)
                dictionary.Add("capability", Capability.ToJson());
            if (Timestamp.HasValue)
                dictionary.Add("timestamp", Timestamp.Value.ToUnixTimeInMilliseconds().ToString());

            if (mergeWith != null)
                return dictionary.Merge(mergeWith);

            return dictionary;
        }
    }
}