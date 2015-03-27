using Ably.Realtime;
using Ably.Transport;
using Moq;
using Xunit;

namespace Ably.Tests.Realtime
{
    public class RealtimeTests
    {
        private static readonly string Debug_Key = "123:456";

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
        public void New_Realtime_WithConnectAutomatically_False_DoesNotConnect()
        {
            // Arrange
            Mock<IConnectionManager> mock = new Mock<IConnectionManager>();
            AblyOptions options = new AblyOptions(Debug_Key) { AutoConnect = false };

            // Act
            AblyRealtime realtime = new AblyRealtime(options, mock.Object);

            // Assert
            Assert.Equal<ConnectionState>(ConnectionState.Initialized, realtime.Connection.State);
            mock.Verify(c => c.Connect(), Times.Never());
        }

        [Fact]
        public void New_Realtime_WithConnectAutomatically_True_ConnectsAutomatically()
        {
            // Arrange
            Mock<IConnectionManager> mock = new Mock<IConnectionManager>();
            AblyOptions options = new AblyOptions(Debug_Key);

            // Act
            AblyRealtime realtime = new AblyRealtime(options, mock.Object);

            // Assert
            Assert.Equal<ConnectionState>(ConnectionState.Connecting, realtime.Connection.State);
            mock.Verify(c => c.Connect(), Times.Once());
        }
    }
}
