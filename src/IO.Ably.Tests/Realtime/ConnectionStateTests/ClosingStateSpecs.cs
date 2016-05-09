using System;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class ClosingStateSpecs : AblySpecs
    {
        [Fact]
        public void ShouldHaveClosingState()
        {
            _state.State.Should().Be(ConnectionStateType.Closing);
        }

        [Fact]
        public void SendMessageShouldDoNothing()
        {
            // Act
            _state.SendMessage(new ProtocolMessage(ProtocolMessage.MessageAction.Attach));
        }

        [Fact]
        public void OnConnectCalled_SHouldDoNothing()
        {
            // Act
            _state.Connect();
        }

        [Fact]
        public void CloseCalled_ShouldDoNothing()
        {
            // Act
            _state.Close();
        }

        [Fact]
        public async Task OnTransportDisconnected_ShouldMoveToClosed()
        {
            // Act
            await _state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(TransportState.Closed));

            // Assert
            _context.StateShouldBe<ConnectionClosedState>();
        }

        [Theory]
        [InlineData(TransportState.Closing)]
        [InlineData(TransportState.Connected)]
        [InlineData(TransportState.Connecting)]
        [InlineData(TransportState.Initialized)]
        public async Task OnTransportStateChangeTHatIsNotClosed_ShouldDoNothing(TransportState transportState)
        {
            // Act
            await _state.OnTransportStateChanged(new ConnectionState.TransportStateInfo(transportState));

            // Assert
            _context.ShouldHaveNotChangedState();
        }

        [Theory]
        [InlineData(ProtocolMessage.MessageAction.Ack)]
        [InlineData(ProtocolMessage.MessageAction.Attach)]
        [InlineData(ProtocolMessage.MessageAction.Attached)]
        [InlineData(ProtocolMessage.MessageAction.Close)]
        [InlineData(ProtocolMessage.MessageAction.Connect)]
        [InlineData(ProtocolMessage.MessageAction.Connected)]
        [InlineData(ProtocolMessage.MessageAction.Detach)]
        [InlineData(ProtocolMessage.MessageAction.Detached)]
        [InlineData(ProtocolMessage.MessageAction.Disconnect)]
        [InlineData(ProtocolMessage.MessageAction.Heartbeat)]
        [InlineData(ProtocolMessage.MessageAction.Message)]
        [InlineData(ProtocolMessage.MessageAction.Nack)]
        [InlineData(ProtocolMessage.MessageAction.Presence)]
        [InlineData(ProtocolMessage.MessageAction.Sync)]
        public async Task ShouldNotHandleInboundMessageAction(ProtocolMessage.MessageAction action)
        {
            // Act
            bool result = await _state.OnMessageReceived(new ProtocolMessage(action));

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldHandleInboundClosedMessageAndMoveToClosed()
        {
            // Act
            bool result = await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Closed));

            // Assert
            result.Should().BeTrue();
            _context.StateShouldBe<ConnectionClosedState>();
        }

        [Fact]
        public async Task ShouldHandleInboundErrorMessageAndMoveToFailedState()
        {
            ErrorInfo targetError = new ErrorInfo("test", 123);
            // Act
            bool result = await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = targetError });

            // Assert
            result.Should().BeTrue();
            var targetState = _context.StateShouldBe<ConnectionFailedState>();
            targetState.Error.ShouldBeEquivalentTo(targetError);
        }

        [Fact]
        public async Task ShouldHandleInboundDisconnectedMessageAndGoToDisconnectedState()
        {
            // Act
            bool result = await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Disconnected));

            // Assert
            Assert.True(result);
            _context.StateShouldBe<ConnectionDisconnectedState>();
        }

        [Fact]
        [Trait("spec", "RTN12a")]
        //When the closing state is initialised a Close message is sent
        public async Task OnAttachedToTransport_ShouldSendCloseMessage()
        {
            // Arrange
            var transport = new FakeTransport() { State = TransportState.Connected };
            _context.Transport = transport;
            // Act
            await _state.OnAttachedToContext();

            // Assert
            _context.LastMessageSent.action.Should().Be(ProtocolMessage.MessageAction.Close);
        }

        [Theory]
        [InlineData(TransportState.Closed)]
        [InlineData(TransportState.Closing)]
        [InlineData(TransportState.Connecting)]
        [InlineData(TransportState.Initialized)]
        public async Task WhenTransportIsNotConnected_ShouldGoStraightToClosed(TransportState transportState)
        {
            // Arrange
            _context.Transport = new FakeTransport() { State = transportState };

            // Act
            await _state.OnAttachedToContext();

            // Assert
            _context.StateShouldBe<ConnectionClosedState>();
        }

        [Fact]
        [Trait("spec", "RTN12b")]
        public async Task AfterTimeoutExpires_ShouldForceStateToClosed()
        {
            _context.Transport = new FakeTransport() { State = TransportState.Connected };

            await _state.OnAttachedToContext();
            _timer.StartedWithAction.Should().BeTrue();
            _timer.OnTimeOut();

            _context.StateShouldBe<ConnectionClosedState>();
        }

        [Fact]
        [Trait("spec", "RTN12a")]
        public async Task WhenClosedMessageReceived_ShouldAbortTimerAndMoveToClosedState()
        {
            // Arrange
            _context.Transport = new FakeTransport(TransportState.Connected);

            // Act
            await _state.OnAttachedToContext();
            await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Closed));

            // Assert
            _timer.StartedWithAction.Should().BeTrue();
            _timer.Aborted.Should().BeTrue();
            _context.StateShouldBe<ConnectionClosedState>();
        }

        [Fact]
        public async Task OnErrorReceived_TimerIsAbortedAndStateIsFailedState()
        {
            // Arrange
            _context.Transport = new FakeTransport(TransportState.Connected);

            // Act
            await _state.OnAttachedToContext();
            await _state.OnMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Error));

            // Assert
            _timer.StartedWithAction.Should().BeTrue();
            _timer.Aborted.Should().BeTrue();
            _context.StateShouldBe<ConnectionFailedState>();
        }

        private FakeConnectionContext _context;
        private ConnectionClosingState _state;
        private FakeTimer _timer;

        private ConnectionClosingState GetState(ErrorInfo info = null)
        {
            return new ConnectionClosingState(_context, info, _timer);
        }

        public ClosingStateSpecs(ITestOutputHelper output) : base(output)
        {
            _timer = new FakeTimer();
            _context = new FakeConnectionContext();
            _state = GetState();
        }
    }
}