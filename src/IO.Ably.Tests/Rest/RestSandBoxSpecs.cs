using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    [Collection("AblyRest SandBox Collection")]
    [Trait("requires", "sandbox")]
    public class RestSandBoxSpecses : SandboxSpecs
    {
        public RestSandBoxSpecses(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output) { }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSC6a")]
        public async Task GettingStats_ShouldReturnValidPaginatedResultOfStats(Protocol protocol)
        {
            var client = await GetRestClient(protocol);

            var stats = await client.StatsAsync(new StatsRequestParams());

            stats.Should().NotBeNull();
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSC16")]
        public async Task Time_ShouldReturnAValidDateTimeOffset(Protocol protocol)
        {
            var client = await GetRestClient(protocol);

            var now = await client.TimeAsync();

            now.Should().BeCloseTo(DateTimeOffset.UtcNow, (int)TimeSpan.FromHours(1).TotalMilliseconds);
        }

        [Collection("AblyRest SandBox Collection")]
        [Trait("requires", "sandbox")]
        public class WithTokenAuthAndInvalidToken : RestSandBoxSpecses
        {
            public WithTokenAuthAndInvalidToken(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output) { }

            [Theory]
            [ProtocolData]
            [Trait("spec", "RSC10")]
            public async Task WhenTokenIsRenewable_ShouldRenewToken(Protocol protocol)
            {
                var authClient = await GetRestClient(protocol);
                var almostExpiredToken = await authClient.Auth.RequestTokenAsync(new TokenParams {ClientId = "123", Ttl = TimeSpan.FromSeconds(1)}, null);
                await Task.Delay(TimeSpan.FromSeconds(2));
                
                //Add this to fool the client it is a valid token
                almostExpiredToken.Expires = DateTimeOffset.UtcNow.AddHours(1); 

                //Trying again with the new token
                var client = await GetRestClient(protocol, options =>
                {
                    options.TokenDetails = almostExpiredToken;
                    options.ClientId = "123";
                    options.Key = "";
                    options.AuthCallback = request => authClient.AblyAuth.RequestTokenAsync(request, null);
                });

                await client.StatsAsync();
                client.AblyAuth.CurrentToken.IsValidToken().Should().BeTrue();
            }
        }

        
    }
}