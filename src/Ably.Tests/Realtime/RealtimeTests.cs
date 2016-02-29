using IO.Ably.Realtime;
using IO.Ably.Transport;
using Moq;
using Xunit;

namespace IO.Ably.Tests
{
    public class RealtimeTests
    {
        private static readonly string Debug_Key = "123.456:789";

        [Fact]
        public void When_HostNotSetInOptions_UseBinaryProtocol_TrueByDefault()
        {
            // Arrange
            AblyRealtimeOptions options = new AblyRealtimeOptions();

            // Act
            Assert.True(options.UseBinaryProtocol);
        }

        [Fact]
        public void New_Realtime_HasConnection()
        {
            AblyRealtime realtime = new AblyRealtime(Debug_Key);
            Assert.NotNull(realtime.Connection);
        }

        [Fact]
        public void New_Realtime_HasChannels()
        {
            AblyRealtime realtime = new AblyRealtime(Debug_Key);
            Assert.NotNull(realtime.Channels);
        }

        [Fact]
        public void New_Realtime_HasAuth()
        {
            AblyRealtime realtime = new AblyRealtime(Debug_Key);
            Assert.NotNull(realtime.Auth);
        }
    }
}
