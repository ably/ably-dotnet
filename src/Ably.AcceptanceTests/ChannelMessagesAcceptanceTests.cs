using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IO.Ably.Encryption;
using IO.Ably.Rest;

namespace IO.Ably.AcceptanceTests
{
    public class StatsAcceptanceTests
    {
        public readonly static DateTimeOffset StartInterval = DateHelper.CreateDate(DateTimeOffset.UtcNow.Year - 1, 2, 3, 15, 5);

        //Stats fixtures can be found in StatsFixture.json which is posted to /stats in TestsSetup.cs

        private readonly Protocol _protocol;

        public StatsAcceptanceTests(Protocol protocol)
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

        [TestFixture(Protocol.Json)]
        [TestFixture(Protocol.MsgPack)]
        [Ignore("Ignoring for the time being. Will fix when I get to stats")]
        public class ByMinuteWhenFromSetToStartIntervalAndLimitSetTo1 : StatsAcceptanceTests
        {
            public ByMinuteWhenFromSetToStartIntervalAndLimitSetTo1(Protocol protocol) : base(protocol)
            {
            }

            [OneTimeSetUp]
            public void GetStats()
            {
                var client = GetAbly();
                Stats = client.Stats(new StatsDataRequestQuery() { Start = StartInterval.AddMinutes(-30), Limit = 1 }).Result;

                TestStats = Stats.First();
            }

            public PaginatedResource<Stats> Stats { get; set; }
            public Stats TestStats { get; set; }

            [Test]
            public void RetrievesOnlyOneStat()
            {
                Stats.Should().HaveCount(1);
            }

            [Test]
            public void ShouldReturnCorrectAggregatedMessageData()
            {
                TestStats.All.Messages.Count.Should().Be(40 + 70);
                TestStats.All.Messages.Data.Should().Be(4000 + 7000);
            }

            [Test]
            public void ReturnsAccurateInboundRealtimeAllData()
            {
                TestStats.Inbound.Realtime.All.Count.Should().Be(70);
                TestStats.Inbound.Realtime.All.Data.Should().Be(7000);
            }

            [Test]
            public void ReturnsAccurateInboundRealtimeMessageData()
            {
                TestStats.Inbound.Realtime.Messages.Count.Should().Be(70);
                TestStats.Inbound.Realtime.Messages.Data.Should().Be(7000);
            }

            [Test]
            public void ReturnsAccurateOutboundRealtimeAllData()
            {
                TestStats.Outbound.Realtime.All.Count.Should().Be(40);
                TestStats.Outbound.Realtime.All.Data.Should().Be(4000);
            }

            [Test]
            public void ReturnsAccuratePersistedPresenceAllData()
            {
                TestStats.Persisted.Presence.Count.Should().Be(20);
                TestStats.Persisted.Presence.Data.Should().Be(2000);
            }

            [Test]
            public void ReturnsAccurateConnectionsAllData()
            {
                TestStats.Connections.Tls.Peak.Should().Be(20);
                TestStats.Connections.Tls.Opened.Should().Be(10);
            }

            [Test]
            public void ReturnsAccurateChannelsAllData()
            {
                TestStats.Channels.Peak.Should().Be(50);
                TestStats.Channels.Opened.Should().Be(30);
            }

            [Test]
            public void ReturnsAccurentApiRequestsData()
            {
                TestStats.ApiRequests.Succeeded.Should().Be(50);
                TestStats.ApiRequests.Failed.Should().Be(10);
            }

            [Test]
            public void ReturnsAccurateTokenRequestsData()
            {
                TestStats.TokenRequests.Succeeded.Should().Be(60);
                TestStats.TokenRequests.Failed.Should().Be(20);
            }
        }

    }


    [TestFixture(Protocol.Json)]
    [TestFixture(Protocol.MsgPack)]
    public class ChannelMessagesAcceptanceTests
    {
        private readonly Protocol _protocol;

        public ChannelMessagesAcceptanceTests(Protocol protocol)
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

        
    }
}