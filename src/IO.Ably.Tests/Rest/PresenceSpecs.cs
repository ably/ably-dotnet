using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class PresenceSpecs : MockHttpSpecs
    {
        [Fact]
        public async Task Presence_CreatesGetRequestWithCorrectPath()
        {
            var rest = GetRestClient();

            var channel = rest.Channels.Get("Test");

            var presence = await channel.Presence.Get();

            Assert.Equal(HttpMethod.Get, LastRequest.Method);
            Assert.Equal($"/channels/{channel.Name}/presence", LastRequest.Url);
        }
        public PresenceSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}