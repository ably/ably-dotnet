using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Rest;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class RealtimeSpecs : AblySpecs
    {
        [Fact]
        [Trait("spec", "RTC1")]
        public void UsesSameClientOptionsAsRestClient()
        {
            var options = new ClientOptions(ValidKey);

            var client = new AblyRealtime(options);

            client.Options.Should().BeSameAs(client.RestClient.Options);
        }

        public class RealtimeProperiesSpec : MockHttpRealtimeSpecs
        {
            private AblyRealtime _client;

            [Fact]
            [Trait("spec", "RTC2")]
            public void ShouldAllowAccessToConnectionObject()
            {
                _client.Connection.Should().NotBeNull();
                _client.Connection.Should().BeOfType<Connection>();
            }

            [Fact]
            [Trait("spec", "RTC3")]
            public void ShouldAllowAccessToChannelsObject()
            {
                _client.Channels.Should().NotBeNull();
                (_client.Channels is IChannels<IRealtimeChannel>).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RTC4")]
            public void ShouldHaveAccessToRestAuth()
            {
                _client.Auth.Should().BeSameAs(_client.RestClient.Auth);
            }

            [Fact]
            [Trait("spec", "RTC5a")]
            public void ShouldProxyRestClientStats()
            {
                _client.StatsAsync();
                LastRequest.Url.Should().Contain("stats");
            }

            [Fact]
            [Trait("spec", "RTC5b")]
            public void ShouldImplementTheSameStatsInterfaceAsTheRestClient()
            {
                (_client is IStatsCommands).Should().BeTrue();
            }

            [Fact]
            [Trait("spec", "RTC6a")]
            public async Task ShouldImplementTheTimeFunction()
            {
                try
                {
                    await _client.TimeAsync();
                }
                catch
                {
                    //ignore processing errors and only care about the request
                }
                LastRequest.Url.Should().Contain("time");
            }


            public RealtimeProperiesSpec(ITestOutputHelper output) : base(output)
            {
                _client = GetRealtimeClient();
            }
        }

        [Fact]
        public void Connection_AllowAccessToConnectionObject()
        {
            var client = new AblyRealtime(ValidKey);
            client.Connection.Should().NotBeNull();
        }


        [Fact]
        public void When_HostNotSetInOptions_UseBinaryProtocol_TrueByDefault()
        {
            // Arrange
            ClientOptions options = new ClientOptions();

            // Act
            if (Config.MsgPackEnabled)
            {
                Assert.True(options.UseBinaryProtocol);
            }
        }

        [Fact]
        public void New_Realtime_HasConnection()
        {
            AblyRealtime realtime = new AblyRealtime(ValidKey);
            Assert.NotNull(realtime.Connection);
        }

        [Fact]
        public void New_Realtime_HasChannels()
        {
            AblyRealtime realtime = new AblyRealtime(ValidKey);
            Assert.NotNull(realtime.Channels);
        }

        [Fact]
        public void New_Realtime_HasAuth()
        {
            AblyRealtime realtime = new AblyRealtime(ValidKey);
            Assert.NotNull(realtime.Auth);
        }

        public RealtimeSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}
