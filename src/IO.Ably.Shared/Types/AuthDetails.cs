using MessagePack;
using Newtonsoft.Json;

namespace IO.Ably.Types
{
    /// <summary>
    /// AuthDetails is a type used with an AUTH protocol messages to send authentication details.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class AuthDetails
    {
        /// <summary>
        /// Gets or sets the accessToken.
        /// </summary>
        [Key("accessToken")]
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; }
    }
}
