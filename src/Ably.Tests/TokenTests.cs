using System;
using System.Collections.Generic;
using System.Configuration;
using Xunit;
using System.Runtime.Serialization;
using Xunit.Extensions;
using System.Net.Http;
using Moq;
using Ably.Auth;
using Newtonsoft.Json.Linq;

namespace Ably.Tests
{
    public class TokenTests
    {

        [Fact]
        public void FromJson_ParsesTokenCorrectly()
            {
                string json = @"{
	                            ""access_token"": {
		                            ""id"": ""QF_CjTvDs2kFQMKLwpccEhIkNcKpw5ovPsOnLsOgJMKow5ACXHvCgGzCtcK7"",
		                            ""key"": ""3lJG9Q"",
		                            ""issued_at"": 1359555878,
		                            ""expires"": 1359559478,
		                            ""capability"": {
			                            ""*"": [
				                            ""*""
			                            ]
		                            }
	                            }
                            }";

                var token = Token.fromJSON(JObject.Parse(json));

                Assert.Equal("QF_CjTvDs2kFQMKLwpccEhIkNcKpw5ovPsOnLsOgJMKow5ACXHvCgGzCtcK7", token.Id);
                //Assert.Equal("3lJG9Q", token.ClientId
                Assert.Equal(1359555878, token.IssuedAt.ToUnixTime());
                Assert.Equal(1359559478, token.Expires.ToUnixTime());
                var expectedCapability = new Capability();
                expectedCapability.AddResource("*").AllowAll();
                Assert.Equal(expectedCapability.ToJson(), token.Capability.ToJson());
            }
    }
}
