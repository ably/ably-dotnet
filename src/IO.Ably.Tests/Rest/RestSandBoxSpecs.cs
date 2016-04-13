using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    [Collection("AblyRest SandBox Collection")]
    [Trait("requires", "sandbox")]
    public class RestSandBoxSpecs
    {
        private readonly AblySandboxFixture _fixture;
        protected readonly ITestOutputHelper Output;

        public RestSandBoxSpecs(AblySandboxFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            Output = output;
            //Reset time in case other tests have changed it
            Config.Now = () => DateTimeOffset.UtcNow;
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
            public WithTokenAuthAndInvalidToken(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output) { }

            [Theory]
            [ProtocolData]
            [Trait("spec", "RSC10")]
            public async Task WhenTokenIsRenewable_ShouldRenewToken(Protocol protocol)
            {
                Output.WriteLine("Current time: " + Config.Now());
                var authClient = await GetRestClient(protocol);
                Output.WriteLine("Getting Token to expire in 1 second");
                var almostExpiredToken = await authClient.Auth.RequestToken(new TokenParams {ClientId = "123", Ttl = TimeSpan.FromSeconds(1)}, null);
                Output.WriteLine("Token: " + almostExpiredToken.ToString());
                await Task.Delay(TimeSpan.FromSeconds(2));
                
                //Add this to fool the client it is a valid token
                almostExpiredToken.Expires = DateTimeOffset.UtcNow.AddHours(1); 

                //Trying again with the new token
                var client = await GetRestClient(protocol, options =>
                {
                    options.TokenDetails = almostExpiredToken;
                    options.ClientId = "123";
                    options.Key = "";
                    options.AuthCallback = request => authClient.AblyAuth.RequestToken(request, null);
                });

                await client.Stats();
                client.AblyAuth.HasValidToken().Should().BeTrue();
            }
        }
    }
}