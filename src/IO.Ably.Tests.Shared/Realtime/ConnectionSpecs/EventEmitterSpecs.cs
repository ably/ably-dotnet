using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Tests.Infrastructure;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Trait("spec", "RTE1")]
    [Trait("spec", "RTN4")]
    public class EventEmitterSpecs : ConnectionSpecsBase
    {

        [Fact]
        [Trait("spec", "RTN4a")]
        public void EmittedEventTypesShouldBe()
        {
            var states = Enum.GetNames(typeof(ConnectionState));
            states.ShouldBeEquivalentTo(new[]
            {
                "Initialized",
                "Connecting",
                "Connected",
                "Disconnected",
                "Suspended",
                "Closing",
                "Closed",
                "Failed"
            });
        }

        [Fact]
        [Trait("spec", "RTN4b")]
        [Trait("spec", "RTN4d")]
        [Trait("spec", "RTN4e")]
        public async Task ANewConnectionShouldRaiseConnectingAndConnectedEvents()
        {
            var client = GetClientWithFakeTransport(opts => opts.AutoConnect = false);
            var states = new List<ConnectionState>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                args.Should().BeOfType<ConnectionStateChange>();
                states.Add(args.Current);
            };

            client.Connect();

            //SendConnected Message
            await client.ConnectionManager.OnTransportMessageReceived(
                new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

            states.Should().BeEquivalentTo(new[] { ConnectionState.Connecting, ConnectionState.Connected });
            client.Connection.State.Should().Be(ConnectionState.Connected);
        }

        [Fact]
        [Trait("spec", "RTN4c")]
        [Trait("spec", "RTN4d")]
        [Trait("spec", "RTN4e")]
        public void WhenClosingAConnection_ItShouldRaiseClosingAndClosedEvents()
        {
            var client = GetClientWithFakeTransport();

            //Start collecting events after the connection is open
            var states = new List<ConnectionState>();
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                args.Should().BeOfType<ConnectionStateChange>();
                states.Add(args.Current);
            };
            LastCreatedTransport.SendAction = message =>
            {
                if (message.Original.Action == ProtocolMessage.MessageAction.Close)
                {
                    LastCreatedTransport.Close(false);
                }
            };

            client.Close();
            states.Should().BeEquivalentTo(new[] { ConnectionState.Closing, ConnectionState.Closed });
            client.Connection.State.Should().Be(ConnectionState.Closed);
        }

        [Fact]
        [Trait("spec", "RTN4f")]
        [Trait("sandboxTest", "needed")]
        public async Task WithAConnectionError_ShouldRaiseChangeStateEventWithError()
        {
            var client = GetClientWithFakeTransport();
            bool hasError = false;
            ErrorInfo actualError = null;
            client.Connection.InternalStateChanged += (sender, args) =>
            {
                hasError = args.HasError;
                actualError = args.Reason;
            };
            var expectedError = new ErrorInfo();

            await client.ConnectionManager.OnTransportMessageReceived(
                new ProtocolMessage(ProtocolMessage.MessageAction.Error) { Error = expectedError });

            hasError.Should().BeTrue();
            actualError.Should().Be(expectedError);
        }

        [Fact]
        [Trait("spec", "RTE3")]
        public async void WithEventEmitter_WhenOn_ListenerRegistersForRepeatedEvents()
        {
            var em = new TestEventEmitter(DefaultLogger.LoggerInstance);
            string m = string.Empty;
            int counter = 0;
            int handledCounter1 = 0;
            int handledCounter2 = 0;
            bool t = false;
            bool tt = false;

            // no event/state argument, catch all
            void Handler1(TestEventEmitterArgs args)
            {
                counter++;
                handledCounter1++;
                m = args.Message;
                t = true;
            }

            void Handler2(TestEventEmitterArgs args)
            {
                counter++;
                handledCounter2++;
                m = args.Message;
                tt = true;
            }

            void Reset()
            {
                t = false;
                tt = false;
            }

            em.On((Action<TestEventEmitterArgs>)Handler1);
            em.DoDummyEmit(1, "on");
            t.Should().BeTrue();
            m.Should().Be("on");

            // currently one listener
            counter.Should().Be(1);
            handledCounter1.Should().Be(1);
            handledCounter2.Should().Be(0);
            Reset();

            // only catch 1 events
            em.On(1, (Action<TestEventEmitterArgs>)Handler2);
            em.DoDummyEmit(2, "on");

            // tt should be false here
            t.Should().BeTrue();
            tt.Should().BeFalse();

            // now there should be 2 listeners, but only the first is catch all
            handledCounter1.Should().Be(2);
            handledCounter2.Should().Be(0);
            counter.Should().Be(2);
            Reset();

            em.DoDummyEmit(1, "on");
            t.Should().BeTrue();
            tt.Should().BeTrue();
            handledCounter1.Should().Be(3);
            handledCounter2.Should().Be(1);

            // still 2 listeners and we sent a 1 event which both should handle
            counter.Should().Be(4);
            Reset();

            // add an existing listener again
            em.On((Action<TestEventEmitterArgs>)Handler2);
            em.DoDummyEmit(1, "on");
            counter.Should().Be(7);
        }

        public EventEmitterSpecs(ITestOutputHelper output) : base(output)
        {
        }

        private class TestEventEmitterArgs : System.EventArgs
        {
            public string Message { get; set; }
        }
        private class TestEventEmitter : EventEmitter<int, TestEventEmitterArgs>
        {
            public TestEventEmitter(ILogger logger) : base(logger) {}
            protected override Action<Action> NotifyClient => action => action();
            public void DoDummyEmit(int state, string message)
            {
                this.Emit(state, new TestEventEmitterArgs() { Message = message });
            }
        }
    }
}