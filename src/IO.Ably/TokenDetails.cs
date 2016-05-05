using System;
using IO.Ably.CustomSerialisers;
using MsgPack.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IO.Ably.Auth
{
    /// <summary>
    /// A class providing details of a token and its associated metadata
    /// </summary>
    public sealed class TokenDetails
    {
        /// <summary>
        /// The token itself
        /// </summary>
        [JsonProperty("token")]
        [MessagePackMember(10, Name = "token")]
        public string Token { get; set; }

        /// <summary>
        /// Absolute token expiry date in UTC
        /// </summary>
        [JsonProperty("expires")]
        [MessagePackMember(30, Name = "expires")]
        [JsonConverter(typeof(DateTimeOffsetJsonConverter))]
        public DateTimeOffset Expires { get; set; }

        /// <summary>
        /// Date and time when the token was issued in UTC
        /// </summary>
        [JsonProperty("issued")]
        [MessagePackMember(40, Name = "issued")]
        [JsonConverter(typeof(DateTimeOffsetJsonConverter))]
        public DateTimeOffset Issued { get; set; }

        /// <summary>
        /// The allowed capabilities for this token. <see cref="Capability"/>
        /// </summary>
        [JsonProperty("capability")]
        [MessagePackMember(50, Name ="capability", NilImplication = NilImplication.MemberDefault)]
        [JsonConverter(typeof(CapabilityJsonConverter))]
        public Capability Capability { get; set; }

        /// <summary>
        /// The clientId associated with the token
        /// </summary>
        [JsonProperty("clientId", NullValueHandling = NullValueHandling.Ignore)]

        [MessagePackMember(60, Name = "clientId")]
        public string ClientId { get; set; }

        public TokenDetails()
        {
        }

        public TokenDetails(string token)
        {
            Token = token;
        }

        public void Expire()
        {
            Expires = Config.Now().AddDays(-1);
        }

        /// <summary>
        /// Checks if a json object is a token. It does it by ensuring the existance of "issued" property
        /// </summary>
        /// <param name="json">Json object to check</param>
        /// <returns>true if json object contains "issued"</returns>
        public static bool IsToken(JObject json)
        {
            return json != null && json["issued"] != null;
        }

        private bool Equals(TokenDetails other)
        {
            return string.Equals(Token, other.Token) && Expires.Equals(other.Expires) && Issued.Equals(other.Issued) && Equals(Capability, other.Capability) && string.Equals(ClientId, other.ClientId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is TokenDetails && Equals((TokenDetails) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Token != null ? Token.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ Expires.GetHashCode();
                hashCode = (hashCode*397) ^ Issued.GetHashCode();
                hashCode = (hashCode*397) ^ (Capability != null ? Capability.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (ClientId != null ? ClientId.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return
                $"Token: {Token}, Expires: {Expires}, Issued: {Issued}, Capability: {Capability}, ClientId: {ClientId}";
        }
    }

    public static class TokenDetailsExtensions
    {
        public static bool IsValidToken(this TokenDetails token)
        {
            if (token == null)
                return false;
            var exp = token.Expires;
            return (exp == DateTimeOffset.MinValue) || (exp >= Config.Now());
        }
    }
}
