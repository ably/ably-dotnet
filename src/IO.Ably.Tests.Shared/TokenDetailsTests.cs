using System;
using Xunit;
using FluentAssertions;
using Newtonsoft.Json.Linq;

namespace IO.Ably.Tests
{
    public class TokenDetailsTests
    {
        [Fact]
        public void FromJson_ParsesTokenCorrectly()
        {
            string json = @"{
	                            ""access_token"": {
		                            ""token"": ""QF_CjTvDs2kFQMKLwpccEhIkNcKpw5ovPsOnLsOgJMKow5ACXHvCgGzCtcK7"",
		                            ""key"": ""3lJG9Q"",
		                            ""issued"": 1430784000000,
		                            ""expires"": 1430784000000,
		                            ""capability"": {
			                            ""*"": [
				                            ""*""
			                            ]
		                            }
	                            }
                            }";

            var token = JsonHelper.DeserializeObject<TokenDetails>((JObject)JObject.Parse(json)["access_token"]);

            Assert.Equal("QF_CjTvDs2kFQMKLwpccEhIkNcKpw5ovPsOnLsOgJMKow5ACXHvCgGzCtcK7", token.Token);

            // Assert.Equal("3lJG9Q", token.ClientId
            Assert.Equal(1430784000000, token.Issued.ToUnixTimeInMilliseconds());
            Assert.Equal(1430784000000, token.Expires.ToUnixTimeInMilliseconds());
            var expectedCapability = new Capability();
            expectedCapability.AddResource("*").AllowAll();
            Assert.Equal(expectedCapability.ToJson(), token.Capability.ToJson());
        }

        [Fact]
        public void ShouldSerializeDatesInMilliseconds()
        {
            var details = new TokenDetails()
            {
                Expires = DateTimeOffset.UtcNow,
                Issued = DateTimeOffset.UtcNow.AddSeconds(1),
            };

            var json = JsonHelper.Serialize(details);

            var jobject = JObject.Parse(json);
            ((string)jobject["expires"]).Should().Be(details.Expires.ToUnixTimeInMilliseconds().ToString());
            ((string)jobject["issued"]).Should().Be(details.Issued.ToUnixTimeInMilliseconds().ToString());
        }

        [Fact]
        public void ShouldExcludeClientIdWhenNull()
        {
            var details = new TokenDetails("123");
            var json = JsonHelper.Serialize(details);
            json.Should().NotContain("clientId");
        }
    }
}
