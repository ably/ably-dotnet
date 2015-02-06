using System;
using MsgPack.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ably.Auth
{
    /// <summary>
    /// A class providing details of a token and its associated metadata
    /// </summary>
    public sealed class Token
    {
        /// <summary>
        /// The token itself
        /// </summary>
        [JsonProperty("id")]
        [MessagePackMember(10, Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("key")]
        [MessagePackMember(20, Name = "key")]
        public string KeyId { get; set; }

        /// <summary>
        /// Absolute token expiry date in UTC
        /// </summary>
        [JsonProperty("expires")]
        [MessagePackMember(30, Name = "expires")]
        public DateTimeOffset ExpiresAt { get; set; }

        /// <summary>
        /// Date and time when the token was issued in UTC
        /// </summary>
        [JsonProperty("issued_at")]
        [MessagePackMember(40, Name = "issued_at")]
        public DateTimeOffset IssuedAt { get; set; }

        /// <summary>
        /// The allowed capabilities for this token. <see cref="Capability"/>
        /// </summary>
        [JsonProperty("capability")]
        [MessagePackMember(50, Name ="capability", NilImplication = NilImplication.MemberDefault)]
        public Capability Capability { get; set; }

        /// <summary>
        /// The clientId associated with the token
        /// </summary>
        [JsonProperty("clientId")]
        [MessagePackMember(60, Name = "clientId")]
        public string ClientId { get; set; }

        public Token()
        {
        }

        public Token(string id)
        {
            Id = id;
        }

        /// <summary>
        /// Checks if a json object is a token. It does it by ensuring the existance of "issued_at" property
        /// </summary>
        /// <param name="json">Json object to check</param>
        /// <returns>true if json object contains "issued_at"</returns>
        public static bool IsToken(JObject json)
        {
            return json != null && json["issued_at"] != null;
        }

        public override string ToString()
        {
            return string.Format("Id: {0}, KeyId: {1}, ExpiresAt: {2}, IssuedAt: {3}, Capability: {4}, ClientId: {5}", Id, KeyId, ExpiresAt, IssuedAt, Capability, ClientId);
        }
    }
}
