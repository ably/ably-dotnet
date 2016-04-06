using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Transport;
using IO.Ably.Types;
using Xunit;

namespace IO.Ably.Tests
{
    //TODO: Make public after fixing Rest tests
    class RealtimeChannelTests
    {
        [Fact]
        public void New_Channel_HasPresence()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();

            // Act
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);

            // Assert
            Assert.NotNull(target.Presence);
        }

        [Fact]
        public void WhenCreated_StateInitialized()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();

            // Act
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);

            // Assert
            Assert.Equal(ChannelState.Initialized, target.State);
        }

        [Fact]
        public void WhenDisconnected_OppensConnectionOnAttach()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);

            // Act
            target.Attach();

            // Assert
            manager.Verify(c => c.Connect(), Times.Once());
        }

        [Fact]
        public void WhenConnected_DoesNotOppenConnectionOnAttach()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);

            // Act
            target.Attach();

            // Assert
            manager.Verify(c => c.Connect(), Times.Never());
        }

        [Fact]
        public void Attach_EmmitsEvent()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);
            List<ChannelState> states = new List<ChannelState>();
            target.ChannelStateChanged += (s, e) => states.Add(e.NewState);

            // Act
            target.Attach();

            // Assert
            Assert.Single<ChannelState>(states, c => c == ChannelState.Attaching);
        }

        [Fact]
        public void Attach_IgnoresSubsequentAttaches()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);
            List<ChannelState> states = new List<ChannelState>();
            target.ChannelStateChanged += (s, e) => states.Add(e.NewState);

            // Act
            target.Attach();
            target.Attach();

            // Assert
            Assert.Single<ChannelState>(states, c => c == ChannelState.Attaching);
        }

        [Fact]
        public void Attach_SendsAttachMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);

            // Act
            target.Attach();

            // Assert
            manager.Verify(c => c.Send(It.Is<ProtocolMessage>(message => message.action == ProtocolMessage.MessageAction.Attach &&
                message.channel == target.Name), null), Times.Once());
        }

        [Fact]
        public void Attach_AttachesSuccessfuly_WhenMessageAttachReceived()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.action == ProtocolMessage.MessageAction.Attach), null))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);

            // Act
            target.Attach();

            // Assert
            Assert.Equal(ChannelState.Attached, target.State);
        }

        [Fact]
        public void Attach_WhenDetaching_MovesStraightToAttaching()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.action == ProtocolMessage.MessageAction.Attach), null))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Task detachingTask = null;
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.action == ProtocolMessage.MessageAction.Detach), null))
                .Callback(() => detachingTask = Task.Factory.StartNew(() => Thread.Sleep(50)).ContinueWith(c => manager.Raise(cc => cc.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Detached))));
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);
            target.Attach();
            target.Detach();

            // Act
            target.Attach();
            detachingTask.Wait();

            // Assert
            Assert.Equal(ChannelState.Attached, target.State);
        }

        [Fact]
        public void Detach_EmmitsEvent()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.action == ProtocolMessage.MessageAction.Attach), null))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);
            List<ChannelState> states = new List<ChannelState>();
            target.Attach();
            target.ChannelStateChanged += (s, e) => states.Add(e.NewState);

            // Act
            target.Detach();

            // Assert
            Assert.Single<ChannelState>(states, c => c == ChannelState.Detaching);
        }

        [Fact]
        public void Detach_IgnoresSubsequentDetaches()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.action == ProtocolMessage.MessageAction.Attach), null))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);
            List<ChannelState> states = new List<ChannelState>();
            target.Attach();
            target.ChannelStateChanged += (s, e) => states.Add(e.NewState);

            // Act
            target.Detach();
            target.Detach();

            // Assert
            Assert.Single<ChannelState>(states, c => c == ChannelState.Detaching);
        }

        [Fact]
        public void Detach_SendsDetachMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.action == ProtocolMessage.MessageAction.Attach), null))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);
            target.Attach();

            // Act
            target.Detach();

            // Assert
            manager.Verify(c => c.Send(It.Is<ProtocolMessage>(message => message.action == ProtocolMessage.MessageAction.Detach &&
                message.channel == target.Name), null), Times.Once());
        }

        [Fact]
        public void Detach_DetachesSuccessfuly_WhenMessageDetachReceived()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.action == ProtocolMessage.MessageAction.Attach), null))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.action == ProtocolMessage.MessageAction.Detach), null))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Detached));
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);
            target.Attach();

            // Act
            target.Detach();

            // Assert
            Assert.Equal(ChannelState.Detached, target.State);
        }

        [Fact]
        public void Detach_WhenAttaching_MovesStraightToDetaching()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            Task attachingTask = null;
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.action == ProtocolMessage.MessageAction.Detach), null))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Detached));
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.action == ProtocolMessage.MessageAction.Attach), null))
                .Callback(() => attachingTask = Task.Factory.StartNew(() => Thread.Sleep(50)).ContinueWith(c => manager.Raise(cc => cc.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached))));
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);
            target.Attach();

            // Act
            target.Detach();
            attachingTask.Wait();

            // Assert
            Assert.Equal(ChannelState.Detached, target.State);
        }

        [Fact]
        public void Detach_WhenStateIsFailed_ThrowsError()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Error));

            // Act
            Assert.Throws<AblyException>(() => target.Detach());
        }

        [Fact]
        public void Publish_WhenAttached_PublishesMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.action == ProtocolMessage.MessageAction.Attach), null))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);
            target.Attach();

            // Act
            target.Publish("message", null);

            // Assert
            manager.Verify(c => c.Send(It.Is<ProtocolMessage>(message => message.action == ProtocolMessage.MessageAction.Message &&
                message.messages.Length == 1 && message.messages[0].name == "message"), null));
        }

        [Fact]
        public void Publish_WhenAttached_PublishesMessages()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.action == ProtocolMessage.MessageAction.Attach), null))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);
            target.Attach();
            Message[] messages = new Message[]
            {
                new Message("message1", null),
                new Message("message2", "payload"),
            };
            ProtocolMessage sendMessage = null;
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(cc => cc.action == ProtocolMessage.MessageAction.Message), null))
                .Callback<ProtocolMessage, Action<bool, ErrorInfo>>((m, e) => sendMessage = m);

            // Act
            target.Publish(messages);

            // Assert
            Assert.Equal(2, sendMessage.messages.Length);
            Assert.Same(messages[0], sendMessage.messages[0]);
            Assert.Same(messages[1], sendMessage.messages[1]);
        }

        [Fact]
        public void Publish_WhenNotAttached_PublishesQueuedMessageOnceAttached()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.action == ProtocolMessage.MessageAction.Attach), null))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);

            // Act
            target.Publish("message", null);
            target.Attach();

            // Assert
            manager.Verify(c => c.Send(It.Is<ProtocolMessage>(message => message.action == ProtocolMessage.MessageAction.Message &&
                message.messages.Length == 1 && message.messages[0].name == "message"), null));
        }

        [Fact]
        public void Publish_WhenNotAttached_PublishesQueuedMessageOnceAttached_AsSingleMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.action == ProtocolMessage.MessageAction.Attach), null))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);

            // Act
            target.Publish("message", null);
            target.Publish("message2", "Test");
            target.Attach();

            // Assert
            manager.Verify(c => c.Send(It.Is<ProtocolMessage>(message => message.action == ProtocolMessage.MessageAction.Message), null), Times.Once());
        }

        [Fact]
        public void WhenReceiveMessage_MessageReceivedEventCalled()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);
            Message[] receivedMessage = null;
            target.Subscribe( ( m ) => receivedMessage = m );

            // Act
            Message[] targetMessages = new Message[] { new Message("test", null) };
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Message, "test") { messages = targetMessages });

            // Assert
            Assert.Equal(targetMessages, receivedMessage);
        }

        [Fact]
        public void WhenReceiveMessage_MessageSubscribersCalled()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);
            Message[] receivedMessage = null;
            target.Subscribe("test", (m) => receivedMessage = m);

            // Act
            Message[] targetMessages = new Message[] { new Message("test", null), new Message("test2", null) };
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Message, "test") { messages = targetMessages });

            // Assert
            Assert.Equal<int>(1, receivedMessage.Length);
            Assert.Equal<Message>(targetMessages[0], receivedMessage[0]);
        }

        [Fact]
        public void WhenReceiveMessage_WithDifferentName_MessageSubscribersNotCalled()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);
            Message[] receivedMessage = null;

            // Act
            target.Subscribe("test 2", (m) => receivedMessage = m);

            Message[] targetMessages = new Message[] { new Message("test", null), new Message("test2", null) };
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Message, "test") { messages = targetMessages });

            // Assert
            Assert.Null(receivedMessage);
        }

        [Fact]
        public void WhenUnsubscribe_MessageSubscribersNotCalled()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);
            Message[] receivedMessage = null;
            Action<Message[]> action = (m) => receivedMessage = m;
            target.Subscribe("test", action);

            // Act
            target.Unsubscribe("test", action);

            Message[] targetMessages = new Message[] { new Message("test", null), new Message("test2", null) };
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Message, "test") { messages = targetMessages });

            // Assert
            Assert.Null(receivedMessage);
        }

        [Fact]
        public void WhenUnsubscribe_WithWrongName_NoException()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Realtime.Channel target = new Realtime.Channel("test", "client", manager.Object);
            Message[] receivedMessage = null;
            Action<Message[]> action = (m) => receivedMessage = m;
            target.Subscribe("test", action);

            // Act
            target.Unsubscribe("test test", action);
        }
    }
}
