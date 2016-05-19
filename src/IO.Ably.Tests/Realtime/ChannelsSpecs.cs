using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Rest;
using IO.Ably.Tests.Realtime;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    public class ChannelsSpecs : ConnectionSpecsBase
    {
        private AblyRealtime _realtime;
        private IRealtimeChannelCommands Channels => _realtime.Channels;


        [Fact]
        [Trait("spec", "RTS3")]
        [Trait("spec", "RTS3a")]
        public void ShouldGetAChannelByName()
        {
            // Act
            var channel = Channels.Get("test");

            // Assert
            channel.Should().NotBeNull();
        }

        [Fact]
        [Trait("spec", "RTS3a")]
        public void ShouldReturnExistingChannel()
        {
            // Arrange
            var channel = Channels.Get("test");

            // Act
            var channel2 = Channels.Get("test");

            // Assert
            channel.Should().BeSameAs(channel2);
        }

        [Fact]
        [Trait("spec", "RTS3b")]
        public void ShouldCreateChannelWithOptions()
        {
            // Arrange
            var options = new ChannelOptions();

            // Act
            var channel = Channels.Get("test", options);

            // Assert
            Assert.Same(options, channel.Options);
        }

        [Fact]
        [Trait("spec", "RTS3c")]
        public void WithExistingChannelAndOptions_ShouldGetExistingChannelAndupdateOpitons()
        {
            // Arrange
            ChannelOptions options = new ChannelOptions();
            var channel = Channels.Get("test");

            // Act
            var channel2 = Channels.Get("test", options);

            // Assert
            Assert.NotNull(channel2);
            Assert.Same(options, channel2.Options);
        }

        [Fact]
        [Trait("spec", "RTS4")]
        [Trait("spec", "RTS4a")]
        public void Release_ShouldDetachChannel()
        {
            // Arrange
            var channel = Channels.Get("test");
            channel.Attach();

            // Act
            Channels.Release("test");

            // Assert
            Assert.Equal(ChannelState.Detaching, channel.State);
        }

        [Fact]
        [Trait("spec", "RTS4a")]
        public void Release_ShouldNotRemoveChannelBeforeDetached()
        {
            // Arrange
            var channel = Channels.Get("test");
            channel.Attach();

            // Act
            Channels.Release("test");

            // Assert
            Assert.Same(channel, Channels.Single());
        }

        [Fact]
        [Trait("spec", "RTS4a")]
        public async Task Release_ShouldRemoveChannelWhenDetached()
        {
            // Arrange
            var channel = Channels.Get("test");
            channel.Attach();
            Channels.Release("test");

            // Act
            await _realtime.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Detached, "test"));

            await Task.Delay(50);
            // Assert
            Channels.Should().BeEmpty();
        }

        [Fact]
        [Trait("spec", "RTS4a")]
        public void Release_RemovesChannelWhenFailed()
        {
            // Arrange
            var channel = Channels.Get("test");
            channel.Attach();
            Channels.Release("test");

            // Act
            _realtime.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error, "test"));

            // Assert
            Channels.Should().BeEmpty();
        }

        [Fact]
        [Trait("spec", "RTS4a")]
        public void ReleaseAll_ShouldDetachChannel()
        {

            // Arrange
            var channel = Channels.Get("test");
            channel.Attach();

            // Act
            Channels.ReleaseAll();

            // Assert
            Assert.Equal(ChannelState.Detaching, channel.State);
        }

        [Fact]
        [Trait("spec", "RTS4a")]
        public void ReleaseAll_ShouldNotRemoveChannelBeforeDetached()
        {
            // Arrange
            var channel = Channels.Get("test");
            channel.Attach();

            // Act
            Channels.ReleaseAll();

            // Assert
            Assert.Same(channel, Channels.Single());
        }

        [Fact]
        [Trait("spec", "RTS4a")]
        public async Task ReleaseAll_ShouldRemoveChannelWhenDetached()
        {
            // Arrange
            var channel = Channels.Get("test");
            channel.Attach();
            Channels.ReleaseAll();

            // Act
            await _realtime.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Detached, "test"));

            // Assert
            Channels.Should().BeEmpty();
        }

        [Fact]
        [Trait("spec", "RTS4a")]
        public void ReleaseAll_ShouldRemoveChannelWhenFailded()
        {
            // Arrange
            var channel = Channels.Get("test");
            channel.Attach();
            Channels.ReleaseAll();

            // Act
            _realtime.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error, "test"));

            // Assert
            Assert.False(Channels.Any());
        }

        [Fact]
        [Trait("spec", "RTS4a")]
        public void AllowsEnumeration()
        {
            // Arrange
            var channel = Channels.Get("test");

            // Act
            IEnumerator enumerator = (Channels as IEnumerable).GetEnumerator();
            enumerator.MoveNext();

            // Assert
            Assert.Same(channel, enumerator.Current);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        [Trait("spec", "RTS4a")]
        public void AllowsGenericEnumeration()
        {
            // Arrange
            var channel = Channels.Get("test");

            // Act
            IEnumerator<IRealtimeChannel> enumerator = Channels.GetEnumerator();
            enumerator.MoveNext();

            // Assert
            Assert.Same(channel, enumerator.Current);
            Assert.False(enumerator.MoveNext());
        }

        

        public ChannelsSpecs(ITestOutputHelper output) : base(output)
        {
            _realtime = GetConnectedClient();
        }
    }
}
