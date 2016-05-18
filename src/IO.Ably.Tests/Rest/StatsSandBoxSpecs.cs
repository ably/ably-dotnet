using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    [Collection("AblyRest SandBox Collection")]
    [Trait("requires", "sandbox")]
    public class StatsSandBoxSpecs : SandboxSpecs
    {
        public readonly static DateTimeOffset StartInterval = DateHelper.CreateDate(DateTimeOffset.UtcNow.Year - 1, 2, 3, 15, 5);
        
        public async Task<Stats> GetStats(Protocol protocol)
        {
            var client = await GetRestClient(protocol);
            var result = client.Stats(new StatsDataRequestQuery() { Start = StartInterval.AddMinutes(-30), Limit = 1 }).Result;

            return result.First();
        }

        [Theory]
        [ProtocolData]
        public async Task ShouldHaveCorrectStatsAsPerStatsSpec(Protocol protocol)
        {
            Logger.LogLevel = LogLevel.Debug;
            var stats = await GetStats(protocol);
            stats.All.Messages.Count.Should().Be(40 + 70);
            stats.All.Messages.Data.Should().Be(4000 + 7000);
            stats.Inbound.Realtime.All.Count.Should().Be(70);
            stats.Inbound.Realtime.All.Data.Should().Be(7000);
            stats.Inbound.Realtime.Messages.Count.Should().Be(70);
            stats.Inbound.Realtime.Messages.Data.Should().Be(7000);
            stats.Outbound.Realtime.All.Count.Should().Be(40);
            stats.Outbound.Realtime.All.Data.Should().Be(4000);
            stats.Persisted.Presence.Count.Should().Be(20);
            stats.Persisted.Presence.Data.Should().Be(2000);
            stats.Connections.Tls.Peak.Should().Be(20);
            stats.Connections.Tls.Opened.Should().Be(10);
            stats.Channels.Peak.Should().Be(50);
            stats.Channels.Opened.Should().Be(30);
            stats.ApiRequests.Succeeded.Should().Be(50);
            stats.ApiRequests.Failed.Should().Be(10);
            stats.TokenRequests.Succeeded.Should().Be(60);
            stats.TokenRequests.Failed.Should().Be(20);
        }

        public StatsSandBoxSpecs(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }
    }
}