using System;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class DisconnectedStateSpecs : AblySpecs
    {
        private readonly FakeConnectionContext _context;
        private readonly ConnectionDisconnectedState _state;
        private readonly FakeTimer _timer;

        public DisconnectedStateSpecs(ITestOutputHelper output)
            : base(output)
        {
            _context = new FakeConnectionContext();
            _timer = new FakeTimer();
            _state = GetState();
        }

        [Fact]
        public void ShouldHaveDisconnectedTypes()
        {
            _state.State.Should().Be(ConnectionState.Disconnected);
        }

        [Theory]
        [InlineData(ProtocolMessage.MessageAction.Ack)]
        [InlineData(ProtocolMessage.MessageAction.Attach)]
        [InlineData(ProtocolMessage.MessageAction.Attached)]
        [InlineData(ProtocolMessage.MessageAction.Close)]
        [InlineData(ProtocolMessage.MessageAction.Closed)]
        [InlineData(ProtocolMessage.MessageAction.Connect)]
        [InlineData(ProtocolMessage.MessageAction.Connected)]
        [InlineData(ProtocolMessage.MessageAction.Detach)]
        [InlineData(ProtocolMessage.MessageAction.Detached)]
        [InlineData(ProtocolMessage.MessageAction.Disconnect)]
        [InlineData(ProtocolMessage.MessageAction.Disconnected)]
        [InlineData(ProtocolMessage.MessageAction.Error)]
        [InlineData(ProtocolMessage.MessageAction.Heartbeat)]
        [InlineData(ProtocolMessage.MessageAction.Message)]
        [InlineData(ProtocolMessage.MessageAction.Nack)]
        [InlineData(ProtocolMessage.MessageAction.Presence)]
        [InlineData(ProtocolMessage.MessageAction.Sync)]
        public async Task ShouldNotHandleInboundMessageAction(ProtocolMessage.MessageAction action)
        {
            // Arrange
            var state = GetState(ErrorInfo.ReasonClosed);

            // Act
            bool handled = await state.OnMessageReceived(new ProtocolMessage(action), null);

            // Assert
            handled.Should().BeFalse();
        }

        [Fact]
        [Trait("spec", "RTN12d")]
        public void WhenCloseCalled_ShouldTransitionToClosedAndTimerAborted()
        {
            // Arrange
            var state = GetState(ErrorInfo.ReasonClosed);

            // Act
            state.Close();

            // Assert
            _context.ShouldQueueCommand<SetClosedStateCommand>();
            _timer.Aborted.Should().BeTrue();
        }

        [Fact]
        public void WhenConnectCalled_ShouldTransitionToConnecting()
        {
            // Arrange
            var state = GetState(ErrorInfo.ReasonClosed);

            // Act
            var command = state.Connect();

            // Assert
            command.Should().BeOfType<SetConnectingStateCommand>();
        }

        [Fact]
        public async Task AfterAnInterval_ShouldRetryConnection()
        {
            // Arrange
            var transport = new FakeTransport { State = TransportState.Initialized };
            _context.Transport = transport;
            var state = GetState(ErrorInfo.ReasonClosed);

            // Act
            state.StartTimer();
            _timer.OnTimeOut();

            // Assert
            _timer.StartedWithAction.Should().BeTrue();
            _context.ShouldQueueCommand<SetConnectingStateCommand>();
        }

        [Fact]
        [Trait("spec", "RTN14d")]
        public void StartTimer_ShouldReportTheDelayItActuallyWaits()
        {
            // RTN14d - retryIn is "the time in milliseconds until the next connection attempt", so
            // the value handed to the application is the one the timer was started with, RTB1 backoff
            // and jitter included.
            _state.StartTimer();

            _timer.LastDelay.Should().BeGreaterThan(TimeSpan.Zero);
            _state.RetryIn.Should().Be(_timer.LastDelay);
        }

        // UTS: realtime/unit/RTB1/disconnected-retry-delay-0
        [Fact]
        [Trait("spec", "RTN14d")]
        [Trait("spec", "RTB1")]
        public void StartTimer_ShouldApplyTheBackoffAndJitter()
        {
            _state.StartTimer();

            // RTB1a's coefficient is 1 for the first retry and RTB1b's jitter is 0.8 to 1.0.
            var nominal = _context.RetryTimeout;
            _timer.LastDelay.Should().BeGreaterOrEqualTo(TimeSpan.FromMilliseconds(nominal.TotalMilliseconds * 0.8));
            _timer.LastDelay.Should().BeLessOrEqualTo(nominal);
        }

        [Fact]
        [Trait("spec", "RTN14d")]
        public void WhenRetryingInstantly_ShouldReportNoWaitAndNotStartTheTimer()
        {
            var state = GetState();
            state.RetryInstantly = true;

            state.StartTimer();

            state.RetryIn.Should().Be(TimeSpan.Zero);
            _timer.StartedWithAction.Should().BeFalse();
        }

        private ConnectionDisconnectedState GetState(ErrorInfo error = null)
        {
            return new ConnectionDisconnectedState(_context, error, _timer, Logger);
        }
    }
}
