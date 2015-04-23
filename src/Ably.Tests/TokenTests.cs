using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
		                            ""token"": ""QF_CjTvDs2kFQMKLwpccEhIkNcKpw5ovPsOnLsOgJMKow5ACXHvCgGzCtcK7"",
		                            ""key"": ""3lJG9Q"",
		                            ""issued"": 1359555878,
		                            ""expires"": 1359559478,
		                            ""capability"": {
			                            ""*"": [
				                            ""*""
			                            ]
		                            }
	                            }
                            }";

                var token = ((JObject)JObject.Parse(json)["access_token"]).ToObject<TokenDetails>();

                Assert.Equal("QF_CjTvDs2kFQMKLwpccEhIkNcKpw5ovPsOnLsOgJMKow5ACXHvCgGzCtcK7", token.Token);
                //Assert.Equal("3lJG9Q", token.ClientId
                Assert.Equal(1359555878, token.Issued.ToUnixTime());
                Assert.Equal(1359559478, token.Expires.ToUnixTime());
                var expectedCapability = new Capability();
                expectedCapability.AddResource("*").AllowAll();
                Assert.Equal(expectedCapability.ToJson(), token.Capability.ToJson());
            }
    }
}
