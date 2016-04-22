using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace IO.Ably.AcceptanceTests
{
    [TestFixture(Protocol.Json)]
    [TestFixture(Protocol.MsgPack)]
    public class PresenceAcceptanceTests
    {
        private readonly Protocol _protocol;

        public PresenceAcceptanceTests(Protocol protocol)
        {
            _protocol = protocol;
        }

        private AblyRest GetAbly()
        {
            var testData = TestsSetup.TestData;

            var options = new ClientOptions
            {
                Key = testData.keys.First().keyStr,
                UseBinaryProtocol = _protocol == Protocol.MsgPack,
                Environment = AblyEnvironment.Sandbox
            };
            var ably = new AblyRest(options);
            return ably;
        }

        [Test]
        public async Task GetsPeoplePresentOnTheChannel()
        {
            string channelName = "persisted:presence_fixtures";
            var ably = GetAbly();
            var channel = ably.Channels.Get(channelName);
             var presence = await channel.Presence.Get();

            presence.Should().HaveCount(4);
            foreach (var presenceMessage in presence)
            {
                presenceMessage.action.Should().Be(PresenceMessage.ActionType.Present);
            }
        }

        [Test]
        public async Task GetsPagedPresenceMessages()
        {
            string channelName = "persisted:presence_fixtures";
            var ably = GetAbly();
            var channel = ably.Channels.Get(channelName);
            var presence = await channel.Presence.History(new DataRequestQuery {Limit=2});

            presence.Should().HaveCount(2);
            presence.HasNext.Should().BeTrue();
            var nextPage = await channel.Presence.History(presence.NextQuery);
            nextPage.Should().HaveCount(2);
        }
    }
}