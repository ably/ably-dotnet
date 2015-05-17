using Ably.Realtime;
using Ably.Transport;
using Ably.Types;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Extensions;

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

        [Fact]
        public void Leave_SendsCorrectMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Stack<ProtocolMessage> messages = new Stack<ProtocolMessage>();
            manager.Setup(c => c.Send(It.IsAny<ProtocolMessage>(), null)).Callback<ProtocolMessage, Action<bool, ErrorInfo>>((m, c) => messages.Push(m));
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            target.Leave(null, null);

            // Assert
            Assert.Equal<int>(1, messages.Count);
            ProtocolMessage msg = messages.Pop();
            Assert.Equal<ProtocolMessage.MessageAction>(ProtocolMessage.MessageAction.Presence, msg.Action);
            Assert.Equal<int>(1, msg.Presence.Length);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Leave, msg.Presence[0].Action);
            Assert.Equal<string>("testClient", msg.Presence[0].ClientId);
        }

        [Fact]
        public void LeaveClient_SendsCorrectMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Stack<ProtocolMessage> messages = new Stack<ProtocolMessage>();
            manager.Setup(c => c.Send(It.IsAny<ProtocolMessage>(), null)).Callback<ProtocolMessage, Action<bool, ErrorInfo>>((m, c) => messages.Push(m));
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            target.LeaveClient("newClient", null, null);

            // Assert
            Assert.Equal<int>(1, messages.Count);
            ProtocolMessage msg = messages.Pop();
            Assert.Equal<ProtocolMessage.MessageAction>(ProtocolMessage.MessageAction.Presence, msg.Action);
            Assert.Equal<int>(1, msg.Presence.Length);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Leave, msg.Presence[0].Action);
            Assert.Equal<string>("newClient", msg.Presence[0].ClientId);
        }

        [Fact]
        public void Update_SendsCorrectMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Stack<ProtocolMessage> messages = new Stack<ProtocolMessage>();
            manager.Setup(c => c.Send(It.IsAny<ProtocolMessage>(), null)).Callback<ProtocolMessage, Action<bool, ErrorInfo>>((m, c) => messages.Push(m));
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            target.Update(null, null);

            // Assert
            Assert.Equal<int>(1, messages.Count);
            ProtocolMessage msg = messages.Pop();
            Assert.Equal<ProtocolMessage.MessageAction>(ProtocolMessage.MessageAction.Presence, msg.Action);
            Assert.Equal<int>(1, msg.Presence.Length);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Update, msg.Presence[0].Action);
            Assert.Equal<string>("testClient", msg.Presence[0].ClientId);
        }

        [Fact]
        public void UpdateClient_SendsCorrectMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Stack<ProtocolMessage> messages = new Stack<ProtocolMessage>();
            manager.Setup(c => c.Send(It.IsAny<ProtocolMessage>(), null)).Callback<ProtocolMessage, Action<bool, ErrorInfo>>((m, c) => messages.Push(m));
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            target.UpdateClient("newClient", null, null);

            // Assert
            Assert.Equal<int>(1, messages.Count);
            ProtocolMessage msg = messages.Pop();
            Assert.Equal<ProtocolMessage.MessageAction>(ProtocolMessage.MessageAction.Presence, msg.Action);
            Assert.Equal<int>(1, msg.Presence.Length);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Update, msg.Presence[0].Action);
            Assert.Equal<string>("newClient", msg.Presence[0].ClientId);
        }

        [Fact]
        public void Presence_Get_ReturnsEmptyArray()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Assert
            Assert.NotNull(target.Get());
        }

        [Theory]
        [InlineData(PresenceMessage.ActionType.Absent)]
        [InlineData(PresenceMessage.ActionType.Enter)]
        [InlineData(PresenceMessage.ActionType.Leave)]
        [InlineData(PresenceMessage.ActionType.Present)]
        [InlineData(PresenceMessage.ActionType.Update)]
        public void OnPresence_Enter_PresenceIsBroadcast(PresenceMessage.ActionType action)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            var target = new Presence(manager.Object, "testChannel", "testClient");
            List<PresenceMessage> broadcastMessages = new List<PresenceMessage>();
            target.MessageReceived += (msg) => broadcastMessages.AddRange(msg);

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                Presence = new PresenceMessage[] { new PresenceMessage(action, "client1") }
            });

            // Assert
            Assert.Equal<int>(1, broadcastMessages.Count);
            Assert.Equal<PresenceMessage.ActionType>(action, broadcastMessages[0].Action);
            Assert.Equal<string>("client1", broadcastMessages[0].ClientId);
        }

        [Fact]
        public void OnPresence_Enter_PresenceMapIsUpdated()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                Presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Enter, "client1") }
            });

            // Assert
            PresenceMessage[] presence = target.Get();
            Assert.NotNull(presence);
            Assert.Equal<int>(1, presence.Length);
            Assert.Equal<string>("client1", presence[0].ClientId);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Enter, presence[0].Action);
        }

        [Fact]
        public void OnPresence_Present_PresenceMapIsUpdated()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                Presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Present, "client1") }
            });

            // Assert
            PresenceMessage[] presence = target.Get();
            Assert.NotNull(presence);
            Assert.Equal<int>(1, presence.Length);
            Assert.Equal<string>("client1", presence[0].ClientId);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Present, presence[0].Action);
        }

        [Fact]
        public void OnPresence_Update_PresenceMapIsUpdated()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                Presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Update, "client1") }
            });

            // Assert
            PresenceMessage[] presence = target.Get();
            Assert.NotNull(presence);
            Assert.Equal<int>(1, presence.Length);
            Assert.Equal<string>("client1", presence[0].ClientId);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Update, presence[0].Action);
        }

        [Fact]
        public void OnPresence_Leave_PresenceMapIsUpdated()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                Presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Leave, "client1") }
            });

            // Assert
            PresenceMessage[] presence = target.Get();
            Assert.NotNull(presence);
            Assert.Equal<int>(0, presence.Length);
        }

        [Fact]
        public void OnPresence_EnterLeave_PresenceMapIsUpdated()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                Presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Enter, "client1") }
            });
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                Presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Leave, "client1") }
            });

            // Assert
            PresenceMessage[] presence = target.Get();
            Assert.NotNull(presence);
            Assert.Equal<int>(0, presence.Length);
        }

        [Fact]
        public void OnPresence_Absent_PresenceMapIsUpdated()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                Presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Absent, "client1") }
            });

            // Assert
            PresenceMessage[] presence = target.Get();
            Assert.NotNull(presence);
            Assert.Equal<int>(0, presence.Length);
        }

        [Fact]
        public void OnPresence_EnterAbsent_PresenceMapIsUpdated()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                Presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Enter, "client1") }
            });
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                Presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Absent, "client1") }
            });

            // Assert
            PresenceMessage[] presence = target.Get();
            Assert.Equal<int>(1, presence.Length);
            Assert.Equal<string>("client1", presence[0].ClientId);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Enter, presence[0].Action);
        }

        [Fact]
        public void OnPresence_Enter_TwiceWithTheSameClientId_NoError()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                Presence = new PresenceMessage[]
                {
                    new PresenceMessage(PresenceMessage.ActionType.Enter, "client1"),
                    new PresenceMessage(PresenceMessage.ActionType.Enter, "client1"),
                }
            });

            // Assert
            PresenceMessage[] presence = target.Get();
            Assert.NotNull(presence);
            Assert.Equal<int>(1, presence.Length);
            Assert.Equal<string>("client1", presence[0].ClientId);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Enter, presence[0].Action);
        }

        [Fact]
        public void OnPresence_Present_TwiceWithTheSameClientId_NoError()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                Presence = new PresenceMessage[]
                {
                    new PresenceMessage(PresenceMessage.ActionType.Present, "client1"),
                    new PresenceMessage(PresenceMessage.ActionType.Present, "client1"),
                }
            });

            // Assert
            PresenceMessage[] presence = target.Get();
            Assert.NotNull(presence);
            Assert.Equal<int>(1, presence.Length);
            Assert.Equal<string>("client1", presence[0].ClientId);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Present, presence[0].Action);
        }

        [Fact]
        public void OnPresence_Update_TwiceWithTheSameClientId_NoError()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                Presence = new PresenceMessage[]
                {
                    new PresenceMessage(PresenceMessage.ActionType.Update, "client1"),
                    new PresenceMessage(PresenceMessage.ActionType.Update, "client1"),
                }
            });

            // Assert
            PresenceMessage[] presence = target.Get();
            Assert.NotNull(presence);
            Assert.Equal<int>(1, presence.Length);
            Assert.Equal<string>("client1", presence[0].ClientId);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Update, presence[0].Action);
        }

        [Fact]
        public void OnPresence_Leave_TwiceWithTheSameClientId_NoError()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                Presence = new PresenceMessage[]
                {
                    new PresenceMessage(PresenceMessage.ActionType.Leave, "client1"),
                    new PresenceMessage(PresenceMessage.ActionType.Leave, "client1")
                }
            });

            // Assert
            PresenceMessage[] presence = target.Get();
            Assert.NotNull(presence);
            Assert.Equal<int>(0, presence.Length);
        }

        [Fact]
        public void OnPresence_Absent_TwiceWithTheSameClientId_NoError()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            var target = new Presence(manager.Object, "testChannel", "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                Presence = new PresenceMessage[]
                {
                    new PresenceMessage(PresenceMessage.ActionType.Absent, "client1"),
                    new PresenceMessage(PresenceMessage.ActionType.Absent, "client1")
                }
            });

            // Assert
            PresenceMessage[] presence = target.Get();
            Assert.NotNull(presence);
            Assert.Equal<int>(0, presence.Length);
        }
    }
}
