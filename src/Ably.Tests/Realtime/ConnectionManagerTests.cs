using Ably.Realtime;
using Ably.Transport;
using Ably.Types;
using Moq;
using System;
using Xunit;

namespace Ably.Tests.Realtime
{
    public class ConnectionManagerTests
    {
        [Fact]
        public void When_Initialized_CallsConnect()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupGet(c => c.State).Returns(TransportState.Initialized);
            ConnectionManager target = new ConnectionManager(mock.Object);

            // Act
            target.Connect();

            // Assert
            mock.Verify(c => c.Connect(), Times.Once());
        }

        [Fact]
        public void When_AlreadyConnected_DoesNothing()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupGet(c => c.State).Returns(TransportState.Connected);
            ConnectionManager target = new ConnectionManager(mock.Object);

            // Act
            target.Connect();

            // Assert
            mock.Verify(c => c.Connect(), Times.Never());
        }

        [Fact]
        public void Close_When_Initialized_DoesNotSendDisconnect()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupGet(c => c.State).Returns(TransportState.Initialized);
            ConnectionManager target = new ConnectionManager(mock.Object);

            // Act
            target.Close();

            // Assert
            mock.Verify(c => c.Close(true), Times.Never());
            mock.Verify(c => c.Close(false), Times.Once());
        }

        [Fact]
        public void Close_When_Connected_SendsDisconnect()
        {
            // Arrange
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupGet(c => c.State).Returns(TransportState.Connected);
            ConnectionManager target = new ConnectionManager(mock.Object);

            // Act
            target.Close();

            // Assert
            mock.Verify(c => c.Close(true), Times.Once());
            mock.Verify(c => c.Close(false), Times.Never());
        }

        [Fact]
        public void Ping_Sends_Heartbeat()
        {
            // Arrange
            ProtocolMessage result = null;
            Mock<ITransport> mock = new Mock<ITransport>();
            mock.SetupGet(c => c.State).Returns(TransportState.Connected);
            mock.Setup(c => c.Send(It.IsAny<ProtocolMessage>())).Callback<ProtocolMessage>(cc => result = cc);
            ConnectionManager target = new ConnectionManager(mock.Object);

            // Act
            target.Ping();

            // Assert
            Assert.Equal<ProtocolMessage.MessageAction>(ProtocolMessage.MessageAction.Heartbeat, result.Action);
        }
    }
}
