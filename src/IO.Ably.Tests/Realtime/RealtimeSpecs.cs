using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class RealtimeTests : AblySpecs
    {
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

        public RealtimeTests(ITestOutputHelper output) : base(output)
        {
        }
    }
}
