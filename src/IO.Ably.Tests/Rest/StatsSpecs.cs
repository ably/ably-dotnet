using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using static Xunit.Assert;

namespace IO.Ably.Tests
{
    [Collection("AblyRest SandBox Collection")]
    [Trait("requires", "sandbox")]
    public class StatsSandBoxSpecs : SandboxSpecs
    {
        public readonly static DateTimeOffset StartInterval = DateHelper.CreateDate(DateTimeOffset.UtcNow.Year - 1, 2, 3, 15, 5);

        //Stats fixtures can be found in StatsFixture.json which is posted to /stats in TestsSetup.cs

        private readonly Protocol _protocol;

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
        //[Ignore("Ignoring for the time being. Will fix when I get to stats")]
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

            public PaginatedResult<Stats> Stats { get; set; }
            public Stats TestStats { get; set; }

            [Fact]
            public void RetrievesOnlyOneStat()
            {
                Stats.Should().HaveCount(1);
            }

            [Fact]
            public void ShouldReturnCorrectAggregatedMessageData()
            {
                TestStats.All.Messages.Count.Should().Be(40 + 70);
                TestStats.All.Messages.Data.Should().Be(4000 + 7000);
            }

            [Fact]
            public void ReturnsAccurateInboundRealtimeAllData()
            {
                TestStats.Inbound.Realtime.All.Count.Should().Be(70);
                TestStats.Inbound.Realtime.All.Data.Should().Be(7000);
            }

            [Fact]
            public void ReturnsAccurateInboundRealtimeMessageData()
            {
                TestStats.Inbound.Realtime.Messages.Count.Should().Be(70);
                TestStats.Inbound.Realtime.Messages.Data.Should().Be(7000);
            }

            [Fact]
            public void ReturnsAccurateOutboundRealtimeAllData()
            {
                TestStats.Outbound.Realtime.All.Count.Should().Be(40);
                TestStats.Outbound.Realtime.All.Data.Should().Be(4000);
            }

            [Fact]
            public void ReturnsAccuratePersistedPresenceAllData()
            {
                TestStats.Persisted.Presence.Count.Should().Be(20);
                TestStats.Persisted.Presence.Data.Should().Be(2000);
            }

            [Fact]f
            public void ReturnsAccurateConnectionsAllData()
            {
                TestStats.Connections.Tls.Peak.Should().Be(20);
                TestStats.Connections.Tls.Opened.Should().Be(10);
            }

            [Fact]
            public void ReturnsAccurateChannelsAllData()
            {
                TestStats.Channels.Peak.Should().Be(50);
                TestStats.Channels.Opened.Should().Be(30);
            }

            [Fact]
            public void ReturnsAccurentApiRequestsData()
            {
                TestStats.ApiRequests.Succeeded.Should().Be(50);
                TestStats.ApiRequests.Failed.Should().Be(10);
            }

            [Fact]
            public void ReturnsAccurateTokenRequestsData()
            {
                TestStats.TokenRequests.Succeeded.Should().Be(60);
                TestStats.TokenRequests.Failed.Should().Be(20);
            }
        }

        public StatsSandBoxSpecs(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }
    }

    public class StatsSpecs : MockHttpRestSpecs
    {
        [Fact]
        public async Task ShouldCreateRequestToCorrectPath()
        {
            var rest = GetRestClient();

            await rest.Stats();

            Equal(HttpMethod.Get, LastRequest.Method);
            Equal("/stats", LastRequest.Url);
        }


        [Fact]
        public async Task ShouldSetCorrectHeaders()
        {
            var rest = GetRestClient();

            var query = new StatsDataRequestQuery
            {
                Start = Now.AddHours(-1),
                End = Now,
                Direction = QueryDirection.Forwards,
                Limit = 1000
            };

            await rest.Stats(query);

            LastRequest.AssertContainsParameter("start", query.Start.Value.ToUnixTimeInMilliseconds().ToString());
            LastRequest.AssertContainsParameter("end", query.End.Value.ToUnixTimeInMilliseconds().ToString());
            LastRequest.AssertContainsParameter("direction", query.Direction.ToString().ToLower());
            LastRequest.AssertContainsParameter("limit", query.Limit.Value.ToString());
        }

        [Fact]
        public async Task ShouldReturnCorrectFirstAndNextLinks()
        {
            //Arrange
            var rest = GetRestClient(request =>
            {
                var response = new AblyResponse()
                {
                    Headers = DataRequestQueryTests.GetSampleStatsRequestHeaders(),
                    TextResponse = "[{}]"
                };
                return response.ToTask();
            });

            //Act
            var result = await rest.Stats();

            //Assert
            NotNull(result.NextQuery);
            NotNull(result.FirstQuery);
        }

        private async Task ExecuteStatsQuery(StatsDataRequestQuery query)
        {
            var rest = GetRestClient();

            await rest.Stats(query);
        }

        [Theory]
        [Trait("spec", "RSC6b1")]
        [InlineData(0, 0, QueryDirection.Backwards)]
        [InlineData(0, 0, QueryDirection.Forwards)]
        [InlineData(0, 1, QueryDirection.Forwards)]
        [InlineData(0, 1, QueryDirection.Backwards)]
        public async Task ShouldAcceptStartAndEndDateTimes(int startOffset, int endOffset, QueryDirection direction)
        {
            var query = new StatsDataRequestQuery
            {
                Start = Now.AddHours(startOffset),
                End = Now.AddHours(endOffset),
                Direction = direction
            };

            await ExecuteStatsQuery(query);

            LastRequest.AssertContainsParameter("start", query.Start.Value.ToUnixTimeInMilliseconds().ToString());
            LastRequest.AssertContainsParameter("end", query.End.Value.ToUnixTimeInMilliseconds().ToString());
        }

        [Theory]
        [Trait("spec", "RSC6b1")]
        [InlineData(1, 0, QueryDirection.Forwards)]
        [InlineData(1, 0, QueryDirection.Backwards)]
        public void ShouldThrowIfStartIsGreaterThanEnd(int startOffset, int endOffset, QueryDirection direction)
        {
            var query = new StatsDataRequestQuery
            {
                Start = Now.AddHours(startOffset),
                End = Now.AddHours(endOffset),
                Direction = direction
            };

            ThrowsAsync<AblyException>(() => ExecuteStatsQuery(query));
        }

        [Theory]
        [InlineData(QueryDirection.Forwards)]
        [InlineData(QueryDirection.Backwards)]
        [InlineData(null)]
        [Trait("spec", "RSC6b2")]
        public async Task ShouldPassDirectionToRequestWithBackwardsAsDefault(QueryDirection? direction)
        {
            var query = new StatsDataRequestQuery
            {
                Start = Now,
                End = Now
            };
            if (direction.HasValue)
                query.Direction = direction.Value;

            await ExecuteStatsQuery(query);

            var expectedDirection = direction.GetValueOrDefault(QueryDirection.Backwards).ToString().ToLower();
            LastRequest.AssertContainsParameter("direction", expectedDirection);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(1000)]
        [InlineData(null)]
        [Trait("spec", "RSC6b3")]
        public async Task ShouldPassLimitWithDefaultof100(int? limit)
        {
            var query = new StatsDataRequestQuery();
            if (limit.HasValue)
                query.Limit = limit.Value;

            await ExecuteStatsQuery(query);

            LastRequest.AssertContainsParameter("limit", limit.GetValueOrDefault(100).ToString());
        }

        [Theory]
        [InlineData(-10)]
        [InlineData(1001)]
        [Trait("spec", "RSCb3")]
        public void ShouldThrowIfLimitExceeds1000orLessThan0(int limit)
        {
            ThrowsAsync<AblyException>(() => ExecuteStatsQuery(new StatsDataRequestQuery() {Limit = limit}));
        }

        [Theory]
        [InlineData(StatsBy.Month)]
        [InlineData(StatsBy.Day)]
        [InlineData(StatsBy.Hour)]
        [InlineData(StatsBy.Minute)]
        [InlineData(null)]
        [Trait("spec", "RSC6b4")]
        public async Task ShouldPassStatsByToQueryWithDefaultOfMinute(StatsBy? statsBy)
        {
            var query = new StatsDataRequestQuery();
            if (statsBy.HasValue)
                query.By = statsBy.Value;

            await ExecuteStatsQuery(query);

            LastRequest.AssertContainsParameter("by", statsBy.GetValueOrDefault(StatsBy.Minute).ToString().ToLower());
        }

        public StatsSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}