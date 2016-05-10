using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Trait("spec", "RTN4")]
    public class EventEmitterSpecs : ConnectionSpecsBase
    {
        [Fact]
        [Trait("spec", "RTN4a")]
        public void EmittedEventTypesShouldBe()
        {
            var states = Enum.GetNames(typeof(ConnectionStateType));
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
            var states = new List<ConnectionStateType>();
            client.Connection.ConnectionStateChanged += (sender, args) =>
            {
                args.Should().BeOfType<ConnectionStateChangedEventArgs>();
                states.Add(args.CurrentState);
            };

            client.Connect();

            //SendConnected Message
            await client.ConnectionManager.OnTransportMessageReceived(
                new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

            states.Should().BeEquivalentTo(new[] { ConnectionStateType.Connecting, ConnectionStateType.Connected });
            client.Connection.State.Should().Be(ConnectionStateType.Connected);
        }

        [Fact]
        [Trait("spec", "RTN4c")]
        [Trait("spec", "RTN4d")]
        [Trait("spec", "RTN4e")]
        public void WhenClosingAConnection_ItShouldRaiseClosingAndClosedEvents()
        {
            var client = GetClientWithFakeTransport();

            //Start collecting events after the connection is open
            var states = new List<ConnectionStateType>();
            client.Connection.ConnectionStateChanged += (sender, args) =>
            {
                args.Should().BeOfType<ConnectionStateChangedEventArgs>();
                states.Add(args.CurrentState);
            };
            LastCreatedTransport.SendAction = message =>
            {
                if (message.Original.action == ProtocolMessage.MessageAction.Close)
                {
                    LastCreatedTransport.Close();
                }
            };

            client.Close();
            states.Should().BeEquivalentTo(new[] { ConnectionStateType.Closing, ConnectionStateType.Closed });
            client.Connection.State.Should().Be(ConnectionStateType.Closed);
        }

        [Fact]
        [Trait("spec", "RTN4f")]
        [Trait("sandboxTest", "needed")]
        public async Task WithAConnectionError_ShouldRaiseChangeStateEventWithError()
        {
            var client = GetClientWithFakeTransport();
            bool hasError = false;
            ErrorInfo actualError = null;
            client.Connection.ConnectionStateChanged += (sender, args) =>
            {
                hasError = args.HasError;
                actualError = args.Reason;
            };
            var expectedError = new ErrorInfo();

            await client.ConnectionManager.OnTransportMessageReceived(
                new ProtocolMessage(ProtocolMessage.MessageAction.Error) { error = expectedError });

            hasError.Should().BeTrue();
            actualError.Should().Be(expectedError);
        }

        public EventEmitterSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}