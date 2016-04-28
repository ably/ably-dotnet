using System.Linq;
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
        public void CreateCorrectTransportParameters_UsesConnectionKey()
        {
            // Arrange
            ClientOptions options = new ClientOptions();
            Mock<Connection> connection = new Mock<Connection>();
            connection.SetupProperty(c => c.Key, "123");

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, connection.Object, false);

            // Assert
            Assert.Equal<string>("123", target.ConnectionKey);
        }

        [Fact]
        public void CreateCorrectTransportParameters_UsesConnectionSerial()
        {
            // Arrange
            ClientOptions options = new ClientOptions();
            Mock<Connection> connection = new Mock<Connection>();
            connection.SetupProperty(c => c.Serial, 123);

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, connection.Object, false);

            // Assert
            Assert.Equal<string>("123", target.ConnectionSerial);
        }

        [Fact]
        public void CreateCorrectTransportParameters_UsesDefaultHost()
        {
            // Arrange
            ClientOptions options = new ClientOptions();

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, null, false);

            // Assert
            Assert.Equal<string>(Defaults.RealtimeHost, target.Host);
        }

        [Fact]
        public void CreateCorrectTransportParameters_Fallback_UsesFallbacktHost()
        {
            // Arrange
            ClientOptions options = new ClientOptions();

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, null, true);

            // Assert
            Assert.True(Defaults.FallbackHosts.Contains(target.Host));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void When_HostSetInOptions_CreateTransportParameters_DoesNotModifyIt(bool fallback)
        {
            // Arrange
            ClientOptions options = new ClientOptions();
            options.RealtimeHost = "http://test";

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, null, fallback);

            // Assert
            Assert.Equal<string>(options.RealtimeHost, target.Host);
        }

        [Theory]
        [InlineData(AblyEnvironment.Sandbox)]
        [InlineData(AblyEnvironment.Uat)]
        public void When_EnvironmentSetInOptions_CreateCorrectTransportParameters(AblyEnvironment environment)
        {
            // Arrange
            ClientOptions options = new ClientOptions();
            options.Environment = environment;
            options.RealtimeHost = "test";

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, null, false);

            // Assert
            Assert.Equal<string>(string.Format("{0}-{1}", environment, options.RealtimeHost).ToLower(), target.Host);
        }

        [Theory]
        [InlineData(AblyEnvironment.Sandbox)]
        [InlineData(AblyEnvironment.Uat)]
        public void When_EnvironmentSetInOptions_CreateCorrectTransportParameters_Fallback(AblyEnvironment environment)
        {
            // Arrange
            ClientOptions options = new ClientOptions();
            options.Environment = environment;
            options.RealtimeHost = "test";

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, null, true);

            // Assert
            Assert.Equal<string>(string.Format("{0}-{1}", environment, options.RealtimeHost).ToLower(), target.Host);
        }

        [Fact]
        public void When_EnvironmentSetInOptions_Live_CreateCorrectTransportParameters()
        {
            // Arrange
            ClientOptions options = new ClientOptions();
            options.Environment = AblyEnvironment.Live;
            options.RealtimeHost = "test";

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, null, false);

            // Assert
            Assert.Equal<string>(options.RealtimeHost, target.Host);
        }

        [Fact]
        public void When_EnvironmentSetInOptions_Live_FallbackDoesNotModifyIt()
        {
            // Arrange
            ClientOptions options = new ClientOptions();
            options.Environment = AblyEnvironment.Live;
            options.RealtimeHost = "test";

            // Act
            TransportParams target = ConnectionManager.CreateTransportParameters(options, null, true);

            // Assert
            Assert.Equal<string>(options.RealtimeHost, target.Host);
        }

        public ConnectionSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}
