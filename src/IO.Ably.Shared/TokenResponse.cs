using MessagePack;
using Newtonsoft.Json;

namespace IO.Ably
{
    [MessagePackObject(AllowPrivate = true)]
    internal class TokenResponse
    {
        [Key("access_token")]
        [JsonProperty("access_token")]
        public TokenDetails AccessToken { get; set; }
    }
}
