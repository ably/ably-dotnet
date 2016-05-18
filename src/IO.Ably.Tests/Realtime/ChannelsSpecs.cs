using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Rest;
using IO.Ably.Tests.Realtime;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    public class ChannelSpecs : ConnectionSpecsBase
    {
        [Trait("spec", "RTL2")]
        public class EventEmitterSpecs : ChannelSpecs
        {
            private IRealtimeChannel _channel;

            [Theory]
            [InlineData(ChannelState.Initialized)]
            [InlineData(ChannelState.Attaching)]
            [InlineData(ChannelState.Attached)]
            [InlineData(ChannelState.Detaching)]
            [InlineData(ChannelState.Detached)]
            [InlineData(ChannelState.Failed)]
            [Trait("spec", "RTL2a")]
            [Trait("spec", "RTL2b")]
            public void ShouldEmitTheFollowingStates(ChannelState state)
            {
                _channel.ChannelStateChanged += (sender, args) =>
                {
                    args.NewState.Should().Be(state);
                    _channel.State.Should().Be(state);
                };

                (_channel as RealtimeChannel).SetChannelState(state);
            }

            [Fact]
            [Trait("spec", "RTL2c")]
            public void ShouldEmmitErrorWithTheErrorThatHasOccuredOnTheChannel()
            {
                var error = new ErrorInfo();
                _channel.ChannelStateChanged += (sender, args) =>
                {
                    args.Reason.Should().BeSameAs(error);
                    _channel.Reason.Should().BeSameAs(error);
                };

                (_channel as RealtimeChannel).SetChannelState(ChannelState.Attached, error);
            }

            public EventEmitterSpecs(ITestOutputHelper output) : base(output)
            {
                _channel = GetConnectedClient().Channels.Get("test");
            }
        }

        [Trait("spec", "RTL3")]
        public class ConnectionStateChangeEffectSpecs : ChannelSpecs
        {
            private AblyRealtime _client;
            private IRealtimeChannel _channel;

            [Theory]
            [InlineData(ChannelState.Attached)]
            [InlineData(ChannelState.Attaching)]
            [Trait("spec", "RTL3a")]
            public async Task WhenConnectionFails_AttachingOrAttachedChannelsShouldTrasitionToFailedWithSameError(ChannelState state)
            {
                var error = new ErrorInfo();
                (_channel as RealtimeChannel).SetChannelState(state);
                await _client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) {error = error});

                _client.Connection.State.Should().Be(ConnectionStateType.Failed);
                _channel.State.Should().Be(ChannelState.Failed);
                _channel.Reason.Should().BeSameAs(error);
            }

            [Theory]
            [InlineData(ChannelState.Attached)]
            [InlineData(ChannelState.Attaching)]
            [Trait("spec", "RTL3b")]
            public async Task WhenConnectionIsClosed_AttachingOrAttachedChannelsShouldTrasitionToDetached(ChannelState state)
            {
                (_channel as RealtimeChannel).SetChannelState(state);

                _client.Close();

                await _client.FakeMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Closed));

                _client.Connection.State.Should().Be(ConnectionStateType.Closed);
                _channel.State.Should().Be(ChannelState.Detached);
            }

            [Theory]
            [InlineData(ChannelState.Attached)]
            [InlineData(ChannelState.Attaching)]
            [Trait("spec", "RTL3b")]
            public async Task WhenConnectionIsSuspended_AttachingOrAttachedChannelsShouldTrasitionToDetached(ChannelState state)
            {
                (_channel as RealtimeChannel).SetChannelState(state);

                _client.Close();

                await _client.ConnectionManager.SetState(new ConnectionSuspendedState(_client.ConnectionManager));

                _client.Connection.State.Should().Be(ConnectionStateType.Suspended);
                _channel.State.Should().Be(ChannelState.Detached);
            }

            public ConnectionStateChangeEffectSpecs(ITestOutputHelper output) : base(output)
            {
                _client = GetConnectedClient();
                _channel = _client.Channels.Get("test");
            }
        }

        [Theory]
        [InlineData(ChannelState.Detached)]
        [InlineData(ChannelState.Failed)]
        [Trait("spec", "RTL11")]
        public void WhenDetachedOrFailed_AllQueuedMessagesShouldBeDeletedAndFailCallbackInvoked(ChannelState state)
        {
            var client = GetConnectedClient();
            var channel = client.Channels.Get("test");
            var expectedError = new ErrorInfo();
            channel.Attach();

            channel.Publish("test", "data", (success, error) =>
            {
                success.Should().BeFalse();
                error.Should().BeSameAs(expectedError);
            });

            var realtimeChannel = (channel as RealtimeChannel);
            realtimeChannel.QueuedMessages.Should().HaveCount(1);
            realtimeChannel.SetChannelState(state, expectedError);
            realtimeChannel.QueuedMessages.Should().HaveCount(0);
        }

        [Fact]
        public void ChannelMessagesArePassedToTheChannelAsSoonAsItBecomesAttached()
        {
            var client = GetConnectedClient();

            var channel = client.Channels.Get("test");
            
        }

        public ChannelSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }

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
