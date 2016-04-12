using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace IO.Ably.Tests
{
    [Collection("AblyRest SandBox Collection")]
    [Trait("requires", "sandbox")]
    public class RestSandBoxSpecs
    {
        private readonly AblySandboxFixture _fixture;

        public RestSandBoxSpecs(AblySandboxFixture fixture)
        {
            _fixture = fixture;
        }

        private async Task<AblyRest> GetRestClient(Protocol protocol, Action<ClientOptions> optionsAction = null)
        {
            var settings = await _fixture.GetSettings();
            var defaultOptions = settings.CreateDefaultOptions();
            defaultOptions.UseBinaryProtocol = protocol == Protocol.MsgPack;
            optionsAction?.Invoke(defaultOptions);
            return new AblyRest(defaultOptions);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSC6a")]
        public async Task GettingStats_ShouldReturnValidPaginatedResultOfStats(Protocol protocol)
        {
            var client = await GetRestClient(protocol);

            var stats = await client.Stats(new StatsDataRequestQuery());

            stats.Should().NotBeNull();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSC16")]
        public async Task Time_ShouldReturnAValidDateTimeOffset(Protocol protocol)
        {
            var client = await GetRestClient(protocol);

            var now = await client.Time();

            now.Should().BeCloseTo(DateTimeOffset.UtcNow, (int)TimeSpan.FromHours(1).TotalMilliseconds);
        }

        [Collection("AblyRest SandBox Collection")]
        [Trait("requires", "sandbox")]
        public class WithTokenAuthAndInvalidToken : RestSandBoxSpecs
        {
            public WithTokenAuthAndInvalidToken(AblySandboxFixture fixture) : base(fixture)
            {

            }

            [Theory]
            [ProtocolData]
            [Trait("specs", "RSC9")]
            public async Task WhenTokenIsRenewable_ShouldRenewToken(Protocol protocol)
            {
                var authClient = await GetRestClient(protocol);
                var almostExpiredToken = await authClient.Auth.RequestToken(new TokenParams {ClientId = "123", Ttl = TimeSpan.FromSeconds(1)}, null);

                await Task.Delay(TimeSpan.FromSeconds(2));
                
                //Add this to fool the client it is a valid token
                almostExpiredToken.Expires = DateTimeOffset.UtcNow.AddHours(1); 

                var client = await GetRestClient(protocol, options =>
                {
                    options.TokenDetails = almostExpiredToken;
                    options.ClientId = "123";
                    options.Key = "";
                    options.AuthCallback = async request => await authClient.AblyAuth.RequestToken(request, null);
                });

                await client.Stats();
                client.AblyAuth.HasValidToken().Should().BeTrue();
            }
        }
        
    }
}