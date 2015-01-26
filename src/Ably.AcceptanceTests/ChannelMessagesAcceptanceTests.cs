using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Ably;
using FluentAssertions;

namespace Ably.AcceptanceTests
{
    
    public class StatsAcceptanceTests
    {
        public readonly static DateTimeOffset StartInterval = new DateTimeOffset(DateTime.Now.Year  -1, 2, 3, 15, 5, 0, TimeSpan.Zero);

        //Stats fixtures can be found in StatsFixture.json which is posted to /stats in TestsSetup.cs

        private static bool _statsSetup = false;
        private readonly Protocol _protocol;

        public StatsAcceptanceTests(Protocol protocol)
        {
            _protocol = protocol;
        }

        private Rest GetAbly()
        {
            var testData = TestsSetup.TestData;

            var options = new AblyOptions
            {
                Key = testData.keys.First().keyStr,
                UseBinaryProtocol = _protocol == Protocol.MsgPack,
                Environment = AblyEnvironment.Sandbox
            };
            var ably = new Rest(options);
            return ably;
        }

        [TestFixture(Protocol.Json)]
        [TestFixture(Protocol.MsgPack)]
        public class ByMinuteWhenFromSetToStartIntervalAndLimitSetTo1 : StatsAcceptanceTests
        {
            public ByMinuteWhenFromSetToStartIntervalAndLimitSetTo1(Protocol protocol) : base(protocol)
            {
            }

            [TestFixtureSetUp]
            public void GetStats()
            {
                var client = GetAbly();
                Stats =  client.Stats(new StatsDataRequestQuery() {Start = StartInterval.AddMinutes(-30), Limit = 1});
                
                TestStats = Stats.First();
            }

            public IPaginatedResource<Stats> Stats { get; set; }
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

        private Rest GetAbly()
        {
            var testData = TestsSetup.TestData;

            var options = new AblyOptions
            {
                Key = testData.keys.First().keyStr,
                UseBinaryProtocol = _protocol == Protocol.MsgPack,
                Environment = AblyEnvironment.Sandbox
            };
            var ably = new Rest(options);
            return ably;
        }

        private JObject examples;

        [SetUp]
        public void Setup()
        {
             examples = JObject.Parse(File.ReadAllText("crypto-data-128.json"));
        }

        public ChannelOptions GetOptions()
        {
            var key = ((string) examples["key"]).FromBase64();
            var iv = ((string)examples["iv"]).FromBase64();
            var keyLength = (int) examples["keylength"];
            var cipherParams = new CipherParams("aes", key, CipherMode.CBC, keyLength, iv);
            return new ChannelOptions(cipherParams);
        }

        [Test]
        public void CanPublishAMessageAndRetrieveIt()
        {
            var items = (JArray) examples["items"];
            
            Ably.Rest ably = GetAbly();
            IChannel channel = ably.Channels.Get("persisted:test", GetOptions());
            var count = 0;
            foreach (var item in items)
            {
                var encoded = item["encoded"];
                var encoding = (string)encoded["encoding"];
                var decodedData = DecodeData((string)encoded["data"], encoding);
                channel.Publish((string)encoded["name"], decodedData);
                var message = channel.History().First();
                if(message.Data is byte[])
                    (message.Data as byte[]).Should().BeEquivalentTo(decodedData as byte[], "Item number {0} data does not match decoded data", count);
                else if (encoding == "json")
                    JToken.DeepEquals((JToken) message.Data, (JToken) decodedData).Should().BeTrue("Item number {0} data does not match decoded data", count);
                else
                    message.Data.Should().Be(decodedData, "Item number {0} data does not match decoded data", count);
                count++;
            }
        }

        [Test]
        public void Send20MessagesAndThenPaginateHistory()
        {
            //Arrange
            Ably.Rest ably = GetAbly();

            IChannel channel = ably.Channels.Get("persisted:historyTest:" + _protocol.ToString());
            //Act
            
            for (int i = 0; i < 20; i++)
            {
                channel.Publish("name" + i, "data" + i);    
            }

            //Assert
            var history = channel.History(new DataRequestQuery() {Limit = 10});
            history.Should().HaveCount(10);
            history.HasNext.Should().BeTrue();
            history.First().Name.Should().Be("name19");

            var secondPage = channel.History(history.NextQuery);
            secondPage.Should().HaveCount(10);
            secondPage.First().Name.Should().Be("name9");


        }

        private object DecodeData(string data, string encoding)
        {
            if (encoding == "json")
            {
                return JsonConvert.DeserializeObject(data);
            }
            else if (encoding == "base64")
                return data.FromBase64();
            else
            {
                return data;
            }
        }
    }
}