using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Rest
{
    [Collection("AblyRest SandBox Collection")]
    [Trait("requires", "sandbox")]
    public class JwtSandboxSpec : SandboxSpecs
    {

        private string _echoServer = "https://echo.ably.io/createJWT";

        private HttpClient _httpClient = new HttpClient();

        public JwtSandboxSpec(AblySandboxFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact]
        public async Task CanGetJwtTokenFromEchoServer()
        {
            var token = await GetJwtTokenAsync();
            token.Should().NotBeNullOrEmpty();
        }

        [Theory]
        [ProtocolData()]
        public async Task WhenJwtTokenEmbedsAblyToken_CanMakeRequest(Protocol protocol)
        {
            var jwt = await GetJwtTokenAsync();
            var client = await GetRestClient(protocol, options =>
            {
                options.TokenDetails = new TokenDetails(jwt);
            });

            var stats = await client.StatsAsync();
            stats.Should().NotBeNull();
        }

        private async Task<string> GetJwtTokenAsync(
            bool invalid = false,
            int expiresIn = 3600,
            string clientId = "testClientIdDotNet",
            string capability = "{\"*\":[\"*\"]}",
            string jwtType = "",
            int encrypted = 0)
        {
            var settings = await Fixture.GetSettings();
            var defaultOptions = settings.CreateDefaultOptions();
            var keyParts = defaultOptions.Key.Split(':');
            if (keyParts.Length != 2)
            {
                throw new Exception($"Cannot create JWT Token. API Key '{defaultOptions.Key}' is not valid.");
            }

            var key = keyParts[0];
            var secret = keyParts[1];

            if (invalid)
            {
                secret = "invalid";
            }

            var builder = new UriBuilder(_echoServer);

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["keyName"] = key;
            query["keySecret"] = secret;
            query["expiresIn"] = expiresIn.ToString();
            query["clientId"] = clientId;
            query["capability"] = capability;
            query["jwtType"] = jwtType;
            query["encrypted"] = encrypted.ToString();
            query["environment"] = "sandbox";
            builder.Query = query.ToQueryString();

            return await _httpClient.GetStringAsync(builder.Uri);
        }
    }
}
