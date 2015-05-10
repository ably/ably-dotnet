using Ably.Realtime;
using Ably.Transport;
using Ably.Types;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace Ably.Tests
{
    public class PresenceTests
    {
        [Fact]
        public void CanCreatePresence()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();

            // Act
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Assert
            Assert.NotNull(target);
        }

        [Fact]
        public void Enter_SendsCorrectMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Stack<ProtocolMessage> messages = new Stack<ProtocolMessage>();
            manager.Setup(c => c.Send(It.IsAny<ProtocolMessage>(), null)).Callback<ProtocolMessage, Action<bool, ErrorInfo>>((m, c) => messages.Push(m));
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            target.Enter(null, null);

            // Assert
            Assert.Equal<int>(1, messages.Count);
            ProtocolMessage msg = messages.Pop();
            Assert.Equal<ProtocolMessage.MessageAction>(ProtocolMessage.MessageAction.Presence, msg.Action);
            Assert.Equal<int>(1, msg.Presence.Length);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Enter, msg.Presence[0].Action);
            Assert.Equal<string>("testClient", msg.Presence[0].ClientId);
        }

        [Fact]
        public void EnterClient_SendsCorrectMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Stack<ProtocolMessage> messages = new Stack<ProtocolMessage>();
            manager.Setup(c => c.Send(It.IsAny<ProtocolMessage>(), null)).Callback<ProtocolMessage, Action<bool, ErrorInfo>>((m, c) => messages.Push(m));
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            target.EnterClient("newClient", null, null);

            // Assert
            Assert.Equal<int>(1, messages.Count);
            ProtocolMessage msg = messages.Pop();
            Assert.Equal<ProtocolMessage.MessageAction>(ProtocolMessage.MessageAction.Presence, msg.Action);
            Assert.Equal<int>(1, msg.Presence.Length);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Enter, msg.Presence[0].Action);
            Assert.Equal<string>("newClient", msg.Presence[0].ClientId);
        }
    }
}
