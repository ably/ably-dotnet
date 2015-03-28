﻿using Ably.Realtime;
using Ably.Transport;
using Ably.Types;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ably.Tests
{
    public class RealtimeChannelTests
    {
        [Fact]
        public void WhenCreated_StateInitialized()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();

            // Act
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);

            // Assert
            Assert.Equal(ChannelState.Initialised, target.State);
        }

        [Fact]
        public void WhenDisconnected_OppensConnectionOnAttach()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);

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
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);

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
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);
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
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);
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
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);

            // Act
            target.Attach();

            // Assert
            manager.Verify(c => c.Send(It.Is<ProtocolMessage>(message => message.Action == ProtocolMessage.MessageAction.Attach &&
                message.Channel == target.Name)), Times.Once());
        }

        [Fact]
        public void Attach_AttachesSuccessfuly_WhenMessageAttachReceived()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.Action == ProtocolMessage.MessageAction.Attach)))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);

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
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.Action == ProtocolMessage.MessageAction.Attach)))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Task detachingTask = null;
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.Action == ProtocolMessage.MessageAction.Detach)))
                .Callback(() => detachingTask = Task.Run(() => Thread.Sleep(50)).ContinueWith(c => manager.Raise(cc => cc.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Detached))));
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);
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
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.Action == ProtocolMessage.MessageAction.Attach)))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);
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
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.Action == ProtocolMessage.MessageAction.Attach)))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);
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
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.Action == ProtocolMessage.MessageAction.Attach)))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);
            target.Attach();

            // Act
            target.Detach();

            // Assert
            manager.Verify(c => c.Send(It.Is<ProtocolMessage>(message => message.Action == ProtocolMessage.MessageAction.Detach &&
                message.Channel == target.Name)), Times.Once());
        }

        [Fact]
        public void Detach_DetachesSuccessfuly_WhenMessageDetachReceived()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.Action == ProtocolMessage.MessageAction.Attach)))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.Action == ProtocolMessage.MessageAction.Detach)))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Detached));
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);
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
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.Action == ProtocolMessage.MessageAction.Detach)))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Detached));
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.Action == ProtocolMessage.MessageAction.Attach)))
                .Callback(() => attachingTask = Task.Run(() => Thread.Sleep(50)).ContinueWith(c => manager.Raise(cc => cc.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached))));
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);
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
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);
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
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.Action == ProtocolMessage.MessageAction.Attach)))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);
            target.Attach();

            // Act
            target.Publish("message", null);

            // Assert
            manager.Verify(c => c.Send(It.Is<ProtocolMessage>(message => message.Action == ProtocolMessage.MessageAction.Message &&
                message.Messages.Length == 1 && message.Messages[0].Name == "message")));
        }

        [Fact]
        public void Publish_WhenAttached_PublishesMessages()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.Action == ProtocolMessage.MessageAction.Attach)))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);
            target.Attach();
            Message[] messages = new Message[]
            {
                new Message("message1", null),
                new Message("message2", "payload"),
            };
            ProtocolMessage sendMessage = null;
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(cc => cc.Action == ProtocolMessage.MessageAction.Message)))
                .Callback<ProtocolMessage>(m => sendMessage = m);

            // Act
            target.Publish(messages);

            // Assert
            Assert.Equal(2, sendMessage.Messages.Length);
            Assert.Same(messages[0], sendMessage.Messages[0]);
            Assert.Same(messages[1], sendMessage.Messages[1]);
        }

        [Fact(Skip="TODO")]
        public void Publish_WhenNotAttached_PublishesQueuedMessageOnceAttached()
        {            
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.Action == ProtocolMessage.MessageAction.Attach)))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);

            // Act
            target.Publish("message", null);
            target.Attach();

            // Assert
            manager.Verify(c => c.Send(It.Is<ProtocolMessage>(message => message.Action == ProtocolMessage.MessageAction.Message &&
                message.Messages.Length == 1 && message.Messages[0].Name == "message")));
        }

        [Fact(Skip = "TODO")]
        public void Publish_WhenNotAttached_PublishesQueuedMessageOnceAttached_AsSingleMessage()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.SetupGet(c => c.IsActive).Returns(true);
            manager.Setup(c => c.Send(It.Is<ProtocolMessage>(m => m.Action == ProtocolMessage.MessageAction.Attach)))
                .Raises(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Attached));
            Realtime.Channel target = new Realtime.Channel("test", manager.Object);

            // Act
            target.Publish("message", null);
            target.Publish("message2", "Test");
            target.Attach();

            // Assert
            manager.Verify(c => c.Send(It.Is<ProtocolMessage>(message => message.Action == ProtocolMessage.MessageAction.Message)), Times.Once());
        }
    }
}
