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
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();

            // Act
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Assert
            Assert.NotNull(target);
        }

        [Fact]
        public void Enter_SendsCorrectMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            channel.SetupGet(c => c.State).Returns(ChannelState.Attached);
            Stack<ProtocolMessage> messages = new Stack<ProtocolMessage>();
            manager.Setup(c => c.Send(It.IsAny<ProtocolMessage>(), null)).Callback<ProtocolMessage, Action<bool, ErrorInfo>>((m, c) => messages.Push(m));
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            target.Enter(null, null);

            // Assert
            Assert.Equal<int>(1, messages.Count);
            ProtocolMessage msg = messages.Pop();
            Assert.Equal<ProtocolMessage.MessageAction>(ProtocolMessage.MessageAction.Presence, msg.action);
            Assert.Equal<int>(1, msg.presence.Length);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Enter, msg.presence[0].Action);
            Assert.Equal<string>("testClient", msg.presence[0].ClientId);
        }

        [Fact]
        public void EnterClient_SendsCorrectMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            channel.SetupGet(c => c.State).Returns(ChannelState.Attached);
            Stack<ProtocolMessage> messages = new Stack<ProtocolMessage>();
            manager.Setup(c => c.Send(It.IsAny<ProtocolMessage>(), null)).Callback<ProtocolMessage, Action<bool, ErrorInfo>>((m, c) => messages.Push(m));
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            target.EnterClient("newClient", null, null);

            // Assert
            Assert.Equal<int>(1, messages.Count);
            ProtocolMessage msg = messages.Pop();
            Assert.Equal<ProtocolMessage.MessageAction>(ProtocolMessage.MessageAction.Presence, msg.action);
            Assert.Equal<int>(1, msg.presence.Length);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Enter, msg.presence[0].Action);
            Assert.Equal<string>("newClient", msg.presence[0].ClientId);
        }

        [Fact]
        public void Leave_SendsCorrectMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            channel.SetupGet(c => c.State).Returns(ChannelState.Attached);
            Stack<ProtocolMessage> messages = new Stack<ProtocolMessage>();
            manager.Setup(c => c.Send(It.IsAny<ProtocolMessage>(), null)).Callback<ProtocolMessage, Action<bool, ErrorInfo>>((m, c) => messages.Push(m));
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            target.Leave(null, null);

            // Assert
            Assert.Equal<int>(1, messages.Count);
            ProtocolMessage msg = messages.Pop();
            Assert.Equal<ProtocolMessage.MessageAction>(ProtocolMessage.MessageAction.Presence, msg.action);
            Assert.Equal<int>(1, msg.presence.Length);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Leave, msg.presence[0].Action);
            Assert.Equal<string>("testClient", msg.presence[0].ClientId);
        }

        [Fact]
        public void LeaveClient_SendsCorrectMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            channel.SetupGet(c => c.State).Returns(ChannelState.Attached);
            Stack<ProtocolMessage> messages = new Stack<ProtocolMessage>();
            manager.Setup(c => c.Send(It.IsAny<ProtocolMessage>(), null)).Callback<ProtocolMessage, Action<bool, ErrorInfo>>((m, c) => messages.Push(m));
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            target.LeaveClient("newClient", null, null);

            // Assert
            Assert.Equal<int>(1, messages.Count);
            ProtocolMessage msg = messages.Pop();
            Assert.Equal<ProtocolMessage.MessageAction>(ProtocolMessage.MessageAction.Presence, msg.action);
            Assert.Equal<int>(1, msg.presence.Length);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Leave, msg.presence[0].Action);
            Assert.Equal<string>("newClient", msg.presence[0].ClientId);
        }

        [Fact]
        public void Update_SendsCorrectMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            channel.SetupGet(c => c.State).Returns(ChannelState.Attached);
            Stack<ProtocolMessage> messages = new Stack<ProtocolMessage>();
            manager.Setup(c => c.Send(It.IsAny<ProtocolMessage>(), null)).Callback<ProtocolMessage, Action<bool, ErrorInfo>>((m, c) => messages.Push(m));
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            target.Update(null, null);

            // Assert
            Assert.Equal<int>(1, messages.Count);
            ProtocolMessage msg = messages.Pop();
            Assert.Equal<ProtocolMessage.MessageAction>(ProtocolMessage.MessageAction.Presence, msg.action);
            Assert.Equal<int>(1, msg.presence.Length);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Update, msg.presence[0].Action);
            Assert.Equal<string>("testClient", msg.presence[0].ClientId);
        }

        [Fact]
        public void UpdateClient_SendsCorrectMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            channel.SetupGet(c => c.State).Returns(ChannelState.Attached);
            Stack<ProtocolMessage> messages = new Stack<ProtocolMessage>();
            manager.Setup(c => c.Send(It.IsAny<ProtocolMessage>(), null)).Callback<ProtocolMessage, Action<bool, ErrorInfo>>((m, c) => messages.Push(m));
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            target.UpdateClient("newClient", null, null);

            // Assert
            Assert.Equal<int>(1, messages.Count);
            ProtocolMessage msg = messages.Pop();
            Assert.Equal<ProtocolMessage.MessageAction>(ProtocolMessage.MessageAction.Presence, msg.action);
            Assert.Equal<int>(1, msg.presence.Length);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Update, msg.presence[0].Action);
            Assert.Equal<string>("newClient", msg.presence[0].ClientId);
        }

        [Fact]
        public void Presence_Get_ReturnsEmptyArray()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            var target = new Presence(manager.Object, channel.Object, "testClient");

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
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            var target = new Presence(manager.Object, channel.Object, "testClient");
            List<PresenceMessage> broadcastMessages = new List<PresenceMessage>();
            target.MessageReceived += (msg) => broadcastMessages.AddRange(msg);

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                presence = new PresenceMessage[] { new PresenceMessage(action, "client1") }
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
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Enter, "client1") }
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
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Present, "client1") }
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
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Update, "client1") }
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
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Leave, "client1") }
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
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Enter, "client1") }
            });
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Leave, "client1") }
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
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Absent, "client1") }
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
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Enter, "client1") }
            });
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                presence = new PresenceMessage[] { new PresenceMessage(PresenceMessage.ActionType.Absent, "client1") }
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
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                presence = new PresenceMessage[]
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
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                presence = new PresenceMessage[]
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
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                presence = new PresenceMessage[]
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
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                presence = new PresenceMessage[]
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
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Presence)
            {
                presence = new PresenceMessage[]
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

        [Fact]
        public void OnSync_2ClientsPresent()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Sync)
            {
                presence = new PresenceMessage[]
                {
                    new PresenceMessage(PresenceMessage.ActionType.Present, "client1"),
                    new PresenceMessage(PresenceMessage.ActionType.Present, "client2")
                }
            });

            // Assert
            PresenceMessage[] presence = target.Get();
            Assert.NotNull(presence);
            Assert.Equal<int>(2, presence.Length);
            Assert.Equal<string>("client1", presence[0].ClientId);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Present, presence[0].Action);
            Assert.Equal<string>("client2", presence[1].ClientId);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Present, presence[1].Action);
        }

        [Fact]
        public void OnSync_1ClientPresentTwice()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Sync)
            {
                presence = new PresenceMessage[]
                {
                    new PresenceMessage(PresenceMessage.ActionType.Present, "client1"),
                    new PresenceMessage(PresenceMessage.ActionType.Present, "client1")
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
        public void OnSync_1ClientPresentTwice_DifferentConnections()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Sync)
            {
                presence = new PresenceMessage[]
                {
                    new PresenceMessage(PresenceMessage.ActionType.Present, "client1") { ConnectionId = "conn123" },
                    new PresenceMessage(PresenceMessage.ActionType.Present, "client1") { ConnectionId = "connAnn332" }
                }
            });

            // Assert
            PresenceMessage[] presence = target.Get();
            Assert.NotNull(presence);
            Assert.Equal<int>(2, presence.Length);
            Assert.Equal<string>("client1", presence[0].ClientId);
            Assert.Equal<string>("conn123", presence[0].ConnectionId);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Present, presence[0].Action);
            Assert.Equal<string>("client1", presence[1].ClientId);
            Assert.Equal<string>("connAnn332", presence[1].ConnectionId);
            Assert.Equal<PresenceMessage.ActionType>(PresenceMessage.ActionType.Present, presence[1].Action);
        }

        [Theory]
        [InlineData(ChannelState.Detached)]
        [InlineData(ChannelState.Detaching)]
        [InlineData(ChannelState.Failed)]
        public void UpdatingPresence_WhenConnection_InvalidState_ThrowsError(ChannelState state)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            channel.SetupGet(c => c.State).Returns(state);
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            Assert.Throws<AblyException>(() => target.Enter(null, null));
        }

        [Fact]
        public void UpdatingPresence_WhenConnection_Initialized_AttachesChannel()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            channel.SetupGet(c => c.State).Returns(ChannelState.Initialized);
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            target.Enter(null, null);

            // Assert
            channel.Verify(c => c.Attach(), Times.Once());
        }

        [Theory]
        [InlineData(ChannelState.Initialized)]
        [InlineData(ChannelState.Attaching)]
        public void UpdatingPresence_WhenConnectionIsConnecting_QueuesMessages(ChannelState state)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            channel.SetupGet(c => c.State).Returns(state);
            var target = new Presence(manager.Object, channel.Object, "testClient");

            // Act
            target.Enter(null, null);

            // Assert
            manager.Verify(c => c.Send(It.IsAny<ProtocolMessage>(), null), Times.Never());
        }

        [Theory]
        [InlineData(ChannelState.Initialized)]
        [InlineData(ChannelState.Attaching)]
        public void UpdatingPresence_WhenChannelIsAttached_SendsQueuedMessages(ChannelState state)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            channel.SetupGet(c => c.State).Returns(state);
            var target = new Presence(manager.Object, channel.Object, "testClient");
            target.Enter(null, null);

            // Act
            channel.Raise(c => c.ChannelStateChanged += null, new ChannelStateChangedEventArgs(ChannelState.Attached));

            // Assert
            manager.Verify(c => c.Send(It.IsAny<ProtocolMessage>(), It.IsAny<Action<bool, ErrorInfo>>()), Times.Once());
        }

        [Theory]
        [InlineData(ChannelState.Initialized)]
        [InlineData(ChannelState.Attaching)]
        public void UpdatingPresence_WhenChannelIsAttached_SendsQueuedMessages_CallsCallbacks(ChannelState state)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            channel.SetupGet(c => c.State).Returns(state);
            manager.Setup(c => c.Send(It.IsAny<ProtocolMessage>(), It.IsAny<Action<bool, ErrorInfo>>()))
                .Callback<ProtocolMessage, Action<bool, ErrorInfo>>((pm, act) => act(true, null));
            var target = new Presence(manager.Object, channel.Object, "testClient");
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();
            target.Enter(null, (s, e) =>
            {
                callbacks.Add(Tuple.Create(s, e));
            });

            // Act
            channel.Raise(c => c.ChannelStateChanged += null, new ChannelStateChangedEventArgs(ChannelState.Attached));

            // Assert
            Assert.Equal<int>(1, callbacks.Count);
            Assert.True(callbacks[0].Item1);
            Assert.Null(callbacks[0].Item2);
        }

        [Theory]
        [InlineData(ChannelState.Initialized)]
        [InlineData(ChannelState.Attaching)]
        public void UpdatingPresence_WhenChannelIsAttached_SendsQueuedMessages_AsASingleMessage(ChannelState state)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            channel.SetupGet(c => c.State).Returns(state);
            var target = new Presence(manager.Object, channel.Object, "testClient");
            target.Enter(null, null);
            target.Update(null, null);

            // Act
            channel.Raise(c => c.ChannelStateChanged += null, new ChannelStateChangedEventArgs(ChannelState.Attached));

            // Assert
            manager.Verify(c => c.Send(It.IsAny<ProtocolMessage>(), It.IsAny<Action<bool, ErrorInfo>>()), Times.Once());
        }

        [Theory]
        [InlineData(ChannelState.Initialized)]
        [InlineData(ChannelState.Attaching)]
        public void UpdatingPresence_WhenChannelIsAttached_SendsQueuedMessages_AsASingleMessage_CallsCallbacks(ChannelState state)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            channel.SetupGet(c => c.State).Returns(state);
            manager.Setup(c => c.Send(It.IsAny<ProtocolMessage>(), It.IsAny<Action<bool, ErrorInfo>>()))
                .Callback<ProtocolMessage, Action<bool, ErrorInfo>>((pm, act) => act(true, null));
            var target = new Presence(manager.Object, channel.Object, "testClient");
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();
            target.Enter(null, (s, e) =>
            {
                callbacks.Add(Tuple.Create(s, e));
            });
            target.Update(null, (s, e) =>
            {
                callbacks.Add(Tuple.Create(s, e));
            });

            // Act
            channel.Raise(c => c.ChannelStateChanged += null, new ChannelStateChangedEventArgs(ChannelState.Attached));

            // Assert
            Assert.Equal<int>(2, callbacks.Count);
            Assert.True(callbacks[0].Item1);
            Assert.Null(callbacks[0].Item2);
            Assert.True(callbacks[1].Item1);
            Assert.Null(callbacks[1].Item2);
        }

        [Theory]
        [InlineData(ChannelState.Initialized)]
        [InlineData(ChannelState.Attaching)]
        public void UpdatingPresence_WhenChannelIsFailed_FailsQueuedMessages(ChannelState state)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            channel.SetupGet(c => c.State).Returns(state);
            var target = new Presence(manager.Object, channel.Object, "testClient");
            target.Enter(null, null);

            // Act
            channel.Raise(c => c.ChannelStateChanged += null, new ChannelStateChangedEventArgs(ChannelState.Failed));

            // Assert
            manager.Verify(c => c.Send(It.IsAny<ProtocolMessage>(), null), Times.Never());
        }

        [Theory]
        [InlineData(ChannelState.Initialized)]
        [InlineData(ChannelState.Attaching)]
        public void UpdatingPresence_WhenChannelIsFailed_FailsQueuedMessages_CallsCallbacks(ChannelState state)
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Mock<IRealtimeChannel> channel = new Mock<IRealtimeChannel>();
            channel.SetupGet(c => c.State).Returns(state);
            var target = new Presence(manager.Object, channel.Object, "testClient");
            ErrorInfo targetError = new ErrorInfo("rrr", 12);
            List<Tuple<bool, ErrorInfo>> callbacks = new List<Tuple<bool, ErrorInfo>>();
            target.Enter(null, (s, e) =>
            {
                callbacks.Add(Tuple.Create(s, e));
            });

            // Act
            channel.Raise(c => c.ChannelStateChanged += null, new ChannelStateChangedEventArgs(ChannelState.Failed, targetError));

            // Assert
            Assert.Equal<int>(1, callbacks.Count);
            Assert.False(callbacks[0].Item1);
            Assert.Same(targetError, callbacks[0].Item2);
        }
    }
}
