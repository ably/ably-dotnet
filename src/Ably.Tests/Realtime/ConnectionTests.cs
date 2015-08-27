using Ably.Realtime;
using Ably.Transport;
using Ably.Types;
using Moq;
using System;
using Xunit;
using Xunit.Extensions;

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

        [Theory]
        [InlineData(ConnectionState.Closed)]
        [InlineData(ConnectionState.Closing)]
        [InlineData(ConnectionState.Connecting)]
        [InlineData(ConnectionState.Disconnected)]
        [InlineData(ConnectionState.Failed)]
        [InlineData(ConnectionState.Initialized)]
        [InlineData(ConnectionState.Suspended)]
        public void When_ConnectionManagerChangesState_StateIsChanged(ConnectionState state)
        {
            // Arrange
            Mock<IConnectionManager> mock = new Mock<IConnectionManager>();
            Connection target = new Connection(mock.Object);

            // Act
            mock.Raise(m => m.StateChanged += null, state, null, null);

            // Assert
            Assert.Equal<ConnectionState>(state, target.State);
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
        public void Ping_Calls_ConnectionManager_Ping()
        {
            // Arrange
            Mock<IConnectionManager> mock = new Mock<IConnectionManager>();
            Connection target = new Connection(mock.Object);

            // Act
            target.Ping();

            // Assert
            mock.Verify(c => c.Send(It.Is<ProtocolMessage>(m => m.Action == ProtocolMessage.MessageAction.Heartbeat), null), Times.Once());
        }
    }
}
