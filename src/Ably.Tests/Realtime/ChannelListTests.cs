using Ably.Realtime;
using Ably.Transport;
using Ably.Types;
using Moq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Ably.Tests
{
    public class ChannelListTests
    {
        [Fact]
        public void GetCreatesChannel()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            ChannelList target = new ChannelList(manager.Object, new Mock<IPresenceFactory>().Object);

            // Act
            var channel = target.Get("test");

            // Assert
            Assert.NotNull(channel);
        }

        [Fact]
        public void GetWillReuseChannelObject()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            ChannelList target = new ChannelList(manager.Object, new Mock<IPresenceFactory>().Object);
            var channel = target.Get("test");

            // Act
            var channel2 = target.Get("test");

            // Assert
            Assert.NotNull(channel2);
            Assert.Same(channel, channel2);
        }

        [Fact]
        public void Release_DetachesChannel()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.Connect()).Raises(c => c.StateChanged += null, ConnectionState.Connected, null, null);
            ChannelList target = new ChannelList(manager.Object, new Mock<IPresenceFactory>().Object);
            var channel = target.Get("test");
            channel.Attach();

            // Act
            target.Release("test");

            // Assert
            Assert.Equal(ChannelState.Detaching, channel.State);
        }

        [Fact]
        public void Release_DoesNotRemoveChannelBeforeDetached()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.Connect()).Raises(c => c.StateChanged += null, ConnectionState.Connected, null, null);
            ChannelList target = new ChannelList(manager.Object, new Mock<IPresenceFactory>().Object);
            var channel = target.Get("test");
            channel.Attach();

            // Act
            target.Release("test");

            // Assert
            Assert.Same(channel, target.Single());
        }

        [Fact]
        public void Release_RemovesChannelWhenDetached()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.Connect()).Raises(c => c.StateChanged += null, ConnectionState.Connected, null, null);
            ChannelList target = new ChannelList(manager.Object, new Mock<IPresenceFactory>().Object);
            var channel = target.Get("test");
            channel.Attach();
            target.Release("test");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Detached, "test"));

            // Assert
            Assert.False(target.Any());
        }

        [Fact]
        public void Release_RemovesChannelWhenFailed()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.Connect()).Raises(c => c.StateChanged += null, ConnectionState.Connected, null, null);
            ChannelList target = new ChannelList(manager.Object, new Mock<IPresenceFactory>().Object);
            var channel = target.Get("test");
            channel.Attach();
            target.Release("test");

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Error, "test"));

            // Assert
            Assert.False(target.Any());
        }

        [Fact]
        public void ReleaseAll_DetachesChannel()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.Connect()).Raises(c => c.StateChanged += null, ConnectionState.Connected, null, null);
            ChannelList target = new ChannelList(manager.Object, new Mock<IPresenceFactory>().Object);
            var channel = target.Get("test");
            channel.Attach();

            // Act
            target.ReleaseAll();

            // Assert
            Assert.Equal(ChannelState.Detaching, channel.State);
        }

        [Fact]
        public void ReleaseAll_DoesNotRemoveChannelBeforeDetached()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.Connect()).Raises(c => c.StateChanged += null, ConnectionState.Connected, null, null);
            ChannelList target = new ChannelList(manager.Object, new Mock<IPresenceFactory>().Object);
            var channel = target.Get("test");
            channel.Attach();

            // Act
            target.ReleaseAll();

            // Assert
            Assert.Same(channel, target.Single());
        }

        [Fact]
        public void ReleaseAll_RemovesChannelWhenDetached()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.Connect()).Raises(c => c.StateChanged += null, ConnectionState.Connected, null, null);
            ChannelList target = new ChannelList(manager.Object, new Mock<IPresenceFactory>().Object);
            var channel = target.Get("test");
            channel.Attach();
            target.ReleaseAll();

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Detached, "test"));

            // Assert
            Assert.False(target.Any());
        }

        [Fact]
        public void ReleaseAll_RemovesChannelWhenFailded()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.Connect()).Raises(c => c.StateChanged += null, ConnectionState.Connected, null, null);
            ChannelList target = new ChannelList(manager.Object, new Mock<IPresenceFactory>().Object);
            var channel = target.Get("test");
            channel.Attach();
            target.ReleaseAll();

            // Act
            manager.Raise(c => c.MessageReceived += null, new ProtocolMessage(ProtocolMessage.MessageAction.Error, "test"));

            // Assert
            Assert.False(target.Any());
        }

        [Fact]
        public void AllowsEnumeration()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.Connect()).Raises(c => c.StateChanged += null, ConnectionState.Connected, null, null);
            ChannelList target = new ChannelList(manager.Object, new Mock<IPresenceFactory>().Object);
            var channel = target.Get("test");

            // Act
            IEnumerator enumerator = (target as IEnumerable).GetEnumerator();
            enumerator.MoveNext();

            // Assert
            Assert.Same(channel, enumerator.Current);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void AllowsGenericEnumeration()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.Connect()).Raises(c => c.StateChanged += null, ConnectionState.Connected, null, null);
            ChannelList target = new ChannelList(manager.Object, new Mock<IPresenceFactory>().Object);
            var channel = target.Get("test");

            // Act
            IEnumerator<Realtime.IRealtimeChannel> enumerator = (target as IEnumerable<Realtime.IRealtimeChannel>).GetEnumerator();
            enumerator.MoveNext();

            // Assert
            Assert.Same(channel, enumerator.Current);
            Assert.False(enumerator.MoveNext());
        }
    }
}
