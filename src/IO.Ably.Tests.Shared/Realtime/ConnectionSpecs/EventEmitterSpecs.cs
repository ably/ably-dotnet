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
        public async void WithEventEmitter_ListenerRegistersForAllEvents()
        {
            var em = new DummyEventEmitter(DefaultLogger.LoggerInstance);
            var t = new TaskCompletionAwaiter(1000);
            string m = "";
            int counter = 0;
            // no event/state argument, catch all
            em.On(args =>
            {
                counter++;
                m = args.Message;
                t.SetCompleted();
            });
            em.DoDummyEmit(1, "on");
            var success = await t.Task;
            success.Should().BeTrue();
            m.Should().Be("on");
            // currently one listener
            counter.Should().Be(1);
            t = new TaskCompletionAwaiter(1000);
            var tt = new TaskCompletionAwaiter(100);
            // only catch 1 events
            em.On(1, args =>
            {
                counter++;
                m = args.Message;
                tt.SetCompleted();
            });

            em.DoDummyEmit(2, "on");
            // tt should timeout and return false here
            success = await t.Task && ! await tt.Task;
            success.Should().BeTrue();
            // now there should be 2 listeners, but only the first is catch all
            counter.Should().Be(2);

            t = new TaskCompletionAwaiter(1000);
            tt = new TaskCompletionAwaiter(1000);

            em.DoDummyEmit(1, "on");
            success = await t.Task && await tt.Task;
            success.Should().BeTrue();
            // still 2 listeners and we sent a 1 event which both should handle
            counter.Should().Be(4);
        }

        public EventEmitterSpecs(ITestOutputHelper output) : base(output)
        {
        }

        private class DummyArgs : System.EventArgs
        {
            public string Message { get; set; }
        }
        private class DummyEventEmitter : EventEmitter<int, DummyArgs>
        {
            public DummyEventEmitter(ILogger logger) : base(logger) {}

            protected override Action<Action> NotifyClient => action => action();

            public void DoDummyEmit(int state, string message)
            {
                this.Emit(state, new DummyArgs() { Message = message });
            }
            
        }
    }
}