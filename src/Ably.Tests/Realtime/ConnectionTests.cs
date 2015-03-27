using Ably.Realtime;
using Ably.Transport;
using Moq;
using System;
using Xunit;

namespace Ably.Tests
{
    public class ConnectionTests
    {
        [Fact]
        public void CreateConnection_StateIsInitialized()
        {
            // Arrange
            Mock<IConnectionManager> mock = new Mock<IConnectionManager>();

            // Act
            Connection target = new Connection(mock.Object);

            // Assert
            Assert.Equal<ConnectionState>(ConnectionState.Initialized, target.State);
        }

        [Fact]
        public void When_Connect_StateIsConnecting()
        {
            // Arrange
            Mock<IConnectionManager> mock = new Mock<IConnectionManager>();
            Connection target = new Connection(mock.Object);

            // Act
            target.Connect();

            // Assert
            Assert.Equal<ConnectionState>(ConnectionState.Connecting, target.State);
        }

        [Fact]
        public void When_Connect_StateIsConnected()
        {
            // Arrange
            Mock<IConnectionManager> mock = new Mock<IConnectionManager>();
            mock.Setup(m => m.Connect()).Raises(m => m.StateChanged += null, ConnectionState.Connected, new ConnectionInfo("1", 2, "3"), null);
            Connection target = new Connection(mock.Object);

            // Act
            target.Connect();

            // Assert
            Assert.Equal<ConnectionState>(ConnectionState.Connected, target.State);
        }

        [Fact]
        public void When_Connected_ConnectionIDPassed()
        {
            // Arrange
            string connectionIdTarget = "123";
            Mock<IConnectionManager> mock = new Mock<IConnectionManager>();
            mock.Setup(m => m.Connect()).Raises(m => m.StateChanged += null, ConnectionState.Connected, new ConnectionInfo(connectionIdTarget, 2, "3"), null);
            Connection target = new Connection(mock.Object);

            // Act
            target.Connect();

            // Assert
            Assert.Equal<string>(connectionIdTarget, target.Id);
        }

        [Fact]
        public void When_Connected_ConnectionSerialPassed()
        {
            // Arrange
            long connectionSerialTarget = 123;
            Mock<IConnectionManager> mock = new Mock<IConnectionManager>();
            mock.Setup(m => m.Connect()).Raises(m => m.StateChanged += null, ConnectionState.Connected, new ConnectionInfo("1", connectionSerialTarget, "3"), null);
            Connection target = new Connection(mock.Object);

            // Act
            target.Connect();

            // Assert
            Assert.Equal<long>(connectionSerialTarget, target.Serial);
        }

        [Fact]
        public void When_Connected_ConnectionKeyPassed()
        {
            // Arrange
            string connectionKeyTarget = "123";
            Mock<IConnectionManager> mock = new Mock<IConnectionManager>();
            mock.Setup(m => m.Connect()).Raises(m => m.StateChanged += null, ConnectionState.Connected, new ConnectionInfo("1", 2, connectionKeyTarget), null);
            Connection target = new Connection(mock.Object);

            // Act
            target.Connect();

            // Assert
            Assert.Equal<string>(connectionKeyTarget, target.Key);
        }

        [Fact]
        public void When_Connected_ErrorInfoPassed()
        {
            // Arrange
            ErrorInfo errorTarget = new ErrorInfo("Error!", 123);
            Mock<IConnectionManager> mock = new Mock<IConnectionManager>();
            mock.Setup(m => m.Connect()).Raises(m => m.StateChanged += null, ConnectionState.Failed, new ConnectionInfo("1", 2, "123"), errorTarget);
            Connection target = new Connection(mock.Object);

            // Act
            target.Connect();

            // Assert
            Assert.Same(errorTarget, target.Reason);
        }

        [Fact]
        public void Close_When_Initialized_GoesToClosing()
        {
            // Arrange
            Mock<IConnectionManager> mock = new Mock<IConnectionManager>();
            Connection target = new Connection(mock.Object);

            // Act
            target.Close();

            // Assert
            Assert.Equal<ConnectionState>(ConnectionState.Closing, target.State);
        }

        [Fact]
        public void Close_When_Initialized_GoesToClosed()
        {
            // Arrange
            Mock<IConnectionManager> mock = new Mock<IConnectionManager>();
            mock.Setup(m => m.Close()).Raises(m => m.StateChanged += null, ConnectionState.Closed, null, null);
            Connection target = new Connection(mock.Object);

            // Act
            target.Close();

            // Assert
            Assert.Equal<ConnectionState>(ConnectionState.Closed, target.State);
        }

        [Fact]
        public void Ping_Calls_ConnectionManager_Ping()
        {
            // Arrange
            Mock<IConnectionManager> mock = new Mock<IConnectionManager>();
            Connection target = new Connection(mock.Object);

            // Act
            target.Ping();

            // Assert
            mock.Verify(c => c.Ping(), Times.Once());
        }
    }
}
