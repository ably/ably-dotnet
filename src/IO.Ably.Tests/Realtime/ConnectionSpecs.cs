using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Transport;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    public class ConnectionSpecs : MockHttpRealtimeSpecs
    {
        [Fact]
        public async Task CreateCorrectTransportParameters_UsesConnectionKey()
        {
            // Arrange
            Mock<Connection> connection = new Mock<Connection>();
            connection.SetupProperty(c => c.Key, "123");
            var manager = new ConnectionManager();
            manager.Connection = connection.Object;

            // Act
            TransportParams target = await manager.CreateTransportParameters();

            // Assert
            Assert.Equal<string>("123", target.ConnectionKey);
        }

        [Fact]
        public async Task CreateCorrectTransportParameters_UsesConnectionSerial()
        {
            // Arrange
            Mock<Connection> connection = new Mock<Connection>();
            connection.SetupProperty(c => c.Serial, 123);
            var manager = new ConnectionManager();
            manager.Connection = connection.Object;


            // Act
            TransportParams target = await manager.CreateTransportParameters();

            // Assert
            target.ConnectionSerial.Should().Be(123);
        }



        public ConnectionSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}
