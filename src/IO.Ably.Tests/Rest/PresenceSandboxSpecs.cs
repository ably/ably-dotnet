using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Encryption;
using IO.Ably.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Rest
{
    [Collection("AblyRest SandBox Collection")]
    [Trait("requires", "sandbox")]
    public class PresenceSandboxSpecs : SandboxSpecs
    {
        public PresenceSandboxSpecs(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
            
        }

        [Theory]
        [ProtocolData]
        public async Task GetsChannelPresence(Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var channel = client.Channels.Get(TestEnvironmentSettings.PresenceChannelName);

            var presence = await channel.Presence.GetAsync();

            presence.Should().HaveCount(6);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSP5")]
        public async Task WithCorrectCipherParams_DecryptsMessagesCorrectly(Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var settings = await Fixture.GetSettings();
            var channel = client.Channels.Get(TestEnvironmentSettings.PresenceChannelName, new ChannelOptions(settings.CipherParams));

            var presence = await channel.Presence.GetAsync();
            foreach (var message in presence)
            {
                message.encoding.Should().BeNullOrEmpty();
            }
        }
    }
}
