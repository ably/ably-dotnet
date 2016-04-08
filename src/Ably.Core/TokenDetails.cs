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

        public TokenDetails(string id)
        {
            Token = id;
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

        public override string ToString()
        {
            return string.Format("Token: {0}, Expires: {1}, Issued: {2}, Capability: {3}, ClientId: {4}", Token, Expires, Issued, Capability, ClientId);
        }
    }
}
