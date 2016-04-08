using System;
using System.Globalization;

namespace IO.Ably
{
    /// <summary>
    /// This is a convenience class for making Token requests. The data held here is used to generate a TokenRequestPostData
    /// object which in turn is serialized and sent to Ably
    /// </summary>
    public class TokenRequest
    {
        /// <summary>
        /// Defaults used when making token requests
        /// </summary>
        public static class Defaults
        {
            public static readonly TimeSpan Ttl = TimeSpan.FromHours(1);
            public static readonly Capability Capability = Capability.AllowAll;
        }

        /// <summary>
        /// The Id against which the request is made
        /// </summary>
        public string KeyName { get; set;}

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
        public DateTime? Timestamp { get; set; }

        /// <summary>
		/// An opaque nonce string of at least 16 characters to ensure
		/// uniqueness of this request. Any subsequent request using the
		/// same nonce will be rejected.
        /// </summary>
        public string Nonce { get; set;}

        internal TokenRequestPostData GetPostData(string keyValue)
        {
            var data = new TokenRequestPostData();
            data.keyName = KeyName;
            data.capability = (Capability ?? Defaults.Capability).ToJson();
            data.clientId = ClientId ?? "";
            DateTime now = Config.Now();
            if (StringExtensions.IsNotEmpty(Nonce))
                data.nonce = Nonce;
            if (Ttl.HasValue)
                data.ttl = Ttl.Value.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            else
                data.ttl = Defaults.Ttl.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            if (Timestamp.HasValue)
                data.timestamp = Timestamp.Value.ToUnixTimeInMilliseconds().ToString();
            else
                data.timestamp = now.ToUnixTimeInMilliseconds().ToString();
            data.CalculateMac(keyValue);

            return data;
        }
    }
}