using Ably.Auth;
using MsgPack.Serialization;
using Newtonsoft.Json;

namespace Ably
{
    internal class TokenResponse
    {
        [JsonProperty("access_token")]
        [MessagePackMember(1, Name = "access_token")]
        public Token AccessToken { get; set; }
    }
}