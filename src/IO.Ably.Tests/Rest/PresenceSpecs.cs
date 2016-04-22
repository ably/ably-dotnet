using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class PresenceSpecs : MockHttpSpecs
    {
        [Fact]
        [Trait("spec", "RSP1")]
        [Trait("spec", "RSP3a")]
        public async Task Presence_CreatesGetRequestWithCorrectPath()
        {
            var rest = GetRestClient();

            var channel = rest.Channels.Get("Test");

            var presence = await channel.Presence.Get();

            presence.Should().BeOfType<PaginatedResource<PresenceMessage>>();

            Assert.Equal(HttpMethod.Get, LastRequest.Method);
            Assert.Equal($"/channels/{channel.Name}/presence", LastRequest.Url);
        }

        [Fact]
        public async Task FactMethodName()
        {
            
        }


        public PresenceSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}