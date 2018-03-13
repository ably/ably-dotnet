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

            // SendConnected Message
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
        public void WithEventEmitter_WhenOn_ListenerRegistersForRepeatedEvents()
        {
            var em = new TestEventEmitter(DefaultLogger.LoggerInstance);
            string message = string.Empty;
            int counter = 0;
            int handledCounter1 = 0;
            int handledCounter2 = 0;
            bool handled1 = false;
            bool handled2 = false;

            // no event/state argument, catch all
            void Handler1(TestEventEmitterArgs args)
            {
                counter++;
                handledCounter1++;
                message = args.Message;
                handled1 = true;
            }

            void Handler2(TestEventEmitterArgs args)
            {
                counter++;
                handledCounter2++;
                message = args.Message;
                handled2 = true;
            }

            void Reset()
            {
                handled1 = false;
                handled2 = false;
            }

            // add Handler1 as a catch all
            em.On((Action<TestEventEmitterArgs>)Handler1);
            em.DoDummyEmit(1, "on");
            handled1.Should().BeTrue();
            message.Should().Be("on");

            // currently one listener
            counter.Should().Be(1);
            handledCounter1.Should().Be(1);
            handledCounter2.Should().Be(0);
            Reset();

            // add another Handler1 as a catch all
            em.On((Action<TestEventEmitterArgs>)Handler1);
            em.DoDummyEmit(1, "on");
            handled1.Should().BeTrue();
            counter.Should().Be(3);
            handledCounter1.Should().Be(3);

            // only catch 1 events
            em.On(1, (Action<TestEventEmitterArgs>)Handler2);
            em.DoDummyEmit(2, "on");

            // handled2 should be false here
            handled1.Should().BeTrue();
            handled2.Should().BeFalse();

            // now there should be 3 listeners, the first is 2 are catch all
            handledCounter1.Should().Be(5);
            handledCounter2.Should().Be(0);
            counter.Should().Be(5);
            Reset();

            em.DoDummyEmit(1, "on");
            handled1.Should().BeTrue();
            handled2.Should().BeTrue();
            handledCounter1.Should().Be(7);
            handledCounter2.Should().Be(1);

            // still 2 listeners and we sent a 1 event which both should handle
            counter.Should().Be(8);
            Reset();
        }

        [Fact]
        [Trait("spec", "RTE4")]
        public void WithEventEmitter_WhenOnce_ListenerRegistersForOneEvent()
        {
            var em = new TestEventEmitter(DefaultLogger.LoggerInstance);
            bool t = false;
            bool tt = false;
            string message = string.Empty;
            int counter = 0;

            void Reset()
            {
                t = false;
                tt = false;
            }

            void Handler1(TestEventEmitterArgs args)
            {
                counter++;
                message = args.Message;
                t = true;
            }

            void Handler2(TestEventEmitterArgs args)
            {
                counter++;
                message = args.Message;
                tt = true;
            }

            // no event/state argument, catch all
            em.Once((Action<TestEventEmitterArgs>)Handler1);
            em.Once((Action<TestEventEmitterArgs>)Handler1);
            em.DoDummyEmit(1, "once");
            t.Should().BeTrue();
            message.Should().Be("once");
            counter.Should().Be(2);
            Reset();

            // only catch 1 events
            em.Once(1, (Action<TestEventEmitterArgs>)Handler2);
            em.DoDummyEmit(2, "on");

            // no events should be handled, t & tt should be false here
            t.Should().BeFalse();
            tt.Should().BeFalse();

            // there are 2 listeners and the first is catch all
            // but the first should have already handled an event and deregistered
            // so the count remains the same
            counter.Should().Be(2);
            Reset();
            em.DoDummyEmit(1, "on");

            // t should not have been set, tt should complete for the first time
            t.Should().BeFalse();
            tt.Should().BeTrue();

            // still 2 listeners and we sent a 1 event which second should handle
            counter.Should().Be(3);
            Reset();
            em.DoDummyEmit(1, "on");

            // t & tt should both be false
            t.Should().BeFalse();
            tt.Should().BeFalse();

            // handlers should be deregistered so the count should remain at 2
            counter.Should().Be(3);
        }

        [Fact]
        [Trait("spec", "RTE5")]
        public void WithEventEmitter_WhenOff_DeRegistersForEvents()
        {
            var em = new TestEventEmitter(DefaultLogger.LoggerInstance);
            string message = string.Empty;
            int counter = 0;
            bool handled = false;

            void Listener1(TestEventEmitterArgs args)
            {
                counter++;
                message = args.Message;
                handled = true;
            }

            void Listener2(TestEventEmitterArgs args)
            {
                counter++;
                message = args.Message;
                handled = true;
            }

            // if called with no arguments, it removes all registrations, for all events and listeners
            em.On(1, (Action<TestEventEmitterArgs>)Listener1);
            em.On(2, (Action<TestEventEmitterArgs>)Listener1);
            em.On(3, (Action<TestEventEmitterArgs>)Listener1);
            em.DoDummyEmit(1, "off");
            handled.Should().BeTrue();
            message.Should().Be("off");
            counter.Should().Be(1);

            handled = false;
            em.Off();
            em.DoDummyEmit(1, "off");
            em.DoDummyEmit(2, "off");
            em.DoDummyEmit(3, "off");
            handled.Should().BeFalse();
            counter.Should().Be(1);

            // if called only with a listener, it removes all registrations matching the given listener,
            // regardless of whether they are associated with an event or not;
            handled = false;
            em.On(1, (Action<TestEventEmitterArgs>)Listener1);
            em.On(1, (Action<TestEventEmitterArgs>)Listener2);
            em.DoDummyEmit(1, "off");
            handled.Should().BeTrue();
            counter.Should().Be(3);

            handled = false;
            em.Off((Action<TestEventEmitterArgs>)Listener2);
            em.DoDummyEmit(1, "off");
            handled.Should().BeTrue();
            counter.Should().Be(4);

            handled = false;
            em.Off((Action<TestEventEmitterArgs>)Listener1);
            em.DoDummyEmit(1, "off");
            handled.Should().BeFalse();
            counter.Should().Be(4);

            // If called with a specific event and a listener,
            // it removes all registrations that match both the given listener and the given event
            em.On(1, (Action<TestEventEmitterArgs>)Listener1);
            em.On(1, (Action<TestEventEmitterArgs>)Listener2);
            em.On(2, (Action<TestEventEmitterArgs>)Listener1);
            handled = false;
            em.DoDummyEmit(1, "off");
            handled.Should().BeTrue();
            counter.Should().Be(6);

            handled = false;
            em.DoDummyEmit(2, "off");
            handled.Should().BeTrue();
            counter.Should().Be(7);

            // no handler for this event, so this should have no effect
            handled = false;
            em.Off(3, (Action<TestEventEmitterArgs>)Listener1);
            em.DoDummyEmit(1, "off");
            handled.Should().BeTrue();
            counter.Should().Be(9);

            // no handler for this listener, so this should have no effect
            handled = false;
            em.Off(2, (Action<TestEventEmitterArgs>)Listener2);
            em.DoDummyEmit(1, "off");
            handled.Should().BeTrue();
            counter.Should().Be(11);

            // remove handler 1 of 2 remaining
            handled = false;
            em.Off(1, (Action<TestEventEmitterArgs>)Listener1);
            em.DoDummyEmit(1, "off");
            handled.Should().BeTrue();
            counter.Should().Be(12);

            // remove the final handler
            handled = false;
            em.Off(1, (Action<TestEventEmitterArgs>)Listener2);
            em.DoDummyEmit(1, "off");
            handled.Should().BeFalse();
            counter.Should().Be(12);
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
            public TestEventEmitter(ILogger logger)
                : base(logger)
            {
            }

            protected override Action<Action> NotifyClient => action => action();

            public void DoDummyEmit(int state, string message)
            {
                Emit(state, new TestEventEmitterArgs() { Message = message });
            }
        }
    }
}