using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Tests.Shared.Helpers;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Rest
{
    [Collection("AblyRest SandBox Collection")]
    [Trait("type", "integration")]
    public class PresenceSandboxSpecs : SandboxSpecs
    {
        public PresenceSandboxSpecs(AblySandboxFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Theory]
        [ProtocolData]
        public async Task GetsChannelPresence(Protocol protocol)
        {
            var client = await GetRestClient(protocol);

            var channel = client.Channels.Get(TestEnvironmentSettings.PresenceChannelName);
            channel.Should().NotBeNull();

            var presence = await channel.Presence.GetAsync();
            presence.Items.Should().HaveCount(6);
        }

        [Theory]
        [ProtocolData]
        [Trait("spec", "RSP5")]
        public async Task WithCorrectCipherParams_DecryptsMessagesCorrectly(Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var settings = await AblySandboxFixture.GetSettings();
            var channel = client.Channels.Get(TestEnvironmentSettings.PresenceChannelName, new ChannelOptions(settings.CipherParams));

            var presence = await channel.Presence.GetAsync();
            presence.Items.Should().HaveCount(6);

            presence.Items[0].Data.Should().Be("true");

            var exampleObject = new JObject
            {
                ["example"] = new JObject { ["json"] = "Object" }
            };
            JAssert.DeepEquals(exampleObject, presence.Items[1].Data as JObject, Output).Should().BeTrue();
            JAssert.DeepEquals(exampleObject, presence.Items[2].Data as JObject, Output).Should().BeTrue();

            presence.Items[3].Data.Should().Be("24");
            presence.Items[4].Data.Should().Be("{ \"test\": \"This is a JSONObject clientData payload\"}");
            presence.Items[5].Data.Should().Be("This is a string clientData payload");
        }
    }
}
