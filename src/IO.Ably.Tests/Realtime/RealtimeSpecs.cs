using FluentAssertions;
using IO.Ably.Realtime;
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
                _client.Channels.Should().BeOfType<ChannelList>();
            }

            [Fact]
            [Trait("spec", "RTC4")]
            public void ShouldHaveAccessToRestAuth()
            {
                _client.Auth.Should().BeSameAs(_client.RestClient.Auth);
            }

            [Fact]
            public void ShouldProxyRestClientStats()
            {
                _client.Stats();
                LastRequest.Url.Should().Contain("stats");
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
            Assert.True(options.UseBinaryProtocol);
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
