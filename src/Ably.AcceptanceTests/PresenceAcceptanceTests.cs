using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Ably.AcceptanceTests
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

        private RestClient GetAbly()
        {
            var testData = TestsSetup.TestData;

            var options = new AblyOptions
            {
                Key = testData.keys.First().keyStr,
                UseBinaryProtocol = _protocol == Protocol.MsgPack,
                Environment = AblyEnvironment.Sandbox
            };
            var ably = new RestClient(options);
            return ably;
        }

        [Test]
        public void GetsPeoplePresentOnTheChannel()
        {
            string channelName = "persisted:presence_fixtures";
            var ably = GetAbly();
            var channel = ably.Channels.Get(channelName);
            var presence = channel.Presence();

            presence.Should().HaveCount(4);
            foreach (var pMessage in presence)
            {
                pMessage.action.Should().Be(PresenceMessage.ActionType.Present);
            }
        }

        [Test]
        public void GetsPagedPresenceMessages()
        {
            string channelName = "persisted:presence_fixtures";
            var ably = GetAbly();
            var channel = ably.Channels.Get(channelName);
            var presence = channel.PresenceHistory(new DataRequestQuery() {Limit=2});

            presence.Should().HaveCount(2);
            presence.HasNext.Should().BeTrue();
            var nextPage = channel.PresenceHistory(presence.NextQuery);
            nextPage.Should().HaveCount(2);
        }
    }
}