using FluentAssertions;
using Xunit;

namespace IO.Ably.Tests.Shared.CustomSerializers
{
    public class JsonSerializationTests
    {
        [Fact]
        public void CanDeserialiseTokenResponse()
        {
            var value = @"{
	            ""access_token"": {
		            ""token"": ""_SYo4Q.D3WmHhU"",
		            ""keyName"": ""_SYo4Q.j8mhAQ"",
		            ""issued"": 1449163326485,
		            ""expires"": 1449163326485,
		            ""capability"": {
			            ""*"": [
				            ""*""
			            ]
		            },
		            ""clientId"": ""123""
	            }
            }";

            var response = JsonHelper.Deserialize<TokenResponse>(value);

            response.AccessToken.Should().NotBeNull();
            response.AccessToken.Capability.ToJson().Should().Be("{\"*\":[\"*\"]}");
            response.AccessToken.ClientId.Should().Be("123");
            response.AccessToken.Token.Should().Be("_SYo4Q.D3WmHhU");
            response.AccessToken.Issued.Should().Be(((long)1449163326485).FromUnixTimeInMilliseconds());
            response.AccessToken.Expires.Should().Be(((long)1449163326485).FromUnixTimeInMilliseconds());
        }
    }
}
