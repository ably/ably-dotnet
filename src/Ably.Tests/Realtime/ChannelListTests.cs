using Ably.Realtime;
using Ably.Transport;
using Moq;
using System.Collections;
using System.Collections.Generic;
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
            ChannelList target = new ChannelList(manager.Object);

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
            ChannelList target = new ChannelList(manager.Object);
            var channel = target.Get("test");

            // Act
            var channel2 = target.Get("test");

            // Assert
            Assert.NotNull(channel2);
            Assert.Same(channel, channel2);
        }

        [Fact]
        public void ReleaseDetachesChannel()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.Connect()).Raises(c => c.StateChanged += null, ConnectionState.Connected, null, null);
            ChannelList target = new ChannelList(manager.Object);
            var channel = target.Get("test");
            channel.Attach();

            // Act
            target.Release("test");

            // Assert
            Assert.Equal(ChannelState.Detaching, channel.State);
        }

        [Fact]
        public void ReleaseAllDetachesChannel()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.Connect()).Raises(c => c.StateChanged += null, ConnectionState.Connected, null, null);
            ChannelList target = new ChannelList(manager.Object);
            var channel = target.Get("test");
            channel.Attach();

            // Act
            target.ReleaseAll();

            // Assert
            Assert.Equal(ChannelState.Detaching, channel.State);
        }

        [Fact]
        public void AllowsEnumeration()
        {
            // Arrange
            Mock<IConnectionManager> manager = new Mock<IConnectionManager>();
            manager.Setup(c => c.Connect()).Raises(c => c.StateChanged += null, ConnectionState.Connected, null, null);
            ChannelList target = new ChannelList(manager.Object);
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
            ChannelList target = new ChannelList(manager.Object);
            var channel = target.Get("test");

            // Act
            IEnumerator<Realtime.Channel> enumerator = (target as IEnumerable<Realtime.Channel>).GetEnumerator();
            enumerator.MoveNext();

            // Assert
            Assert.Same(channel, enumerator.Current);
            Assert.False(enumerator.MoveNext());
        }
    }
}
