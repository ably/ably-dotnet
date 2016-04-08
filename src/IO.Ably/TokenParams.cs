using System;

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

        public TokenParams()
        {
            Capability = Capability.AllowAll;
            Ttl = TimeSpan.FromMinutes(60);
        }
    }
}