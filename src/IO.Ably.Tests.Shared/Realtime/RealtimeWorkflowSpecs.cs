using FluentAssertions;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Transport;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.NETFramework.Realtime
{
    public class RealtimeWorkflowSpecs : AblyRealtimeSpecs
    {
        [Fact]
        [Trait("spec", "RTN8b")]
        public void ConnectedState_UpdatesConnectionInformation()
        {
            // Act
            // TODO: Move this test to the workflow tests
            var connectedProtocolMessage = new ProtocolMessage(ProtocolMessage.MessageAction.Connected)
            {
                ConnectionId = "1",
                ConnectionSerial = 100,
                ConnectionDetails = new ConnectionDetails()
                {
                    ClientId = "client1", ConnectionKey = "validKey"
                },
            };
            var client = GetRealtimeClient(options => options.AutoConnect = false);
            client.Workflow.ProcessCommand(SetConnectedStateCommand.Create(connectedProtocolMessage, false));

            // Assert
            var connection = client.Connection;
            connection.Id.Should().Be("1");
            connection.Serial.Should().Be(100);
            connection.Key.Should().Be("validKey");
            client.Auth.ClientId.Should().Be("client1");
        }

//        [Fact]
//        public void BeforeTransition_ShouldClearConnectionKeyAndId()
//        {
//            // Arrange
//            _context.Connection.Key = "Test";
//            _context.Connection.Id = "Test";
//
//            // Act
//            _state.BeforeTransition();
//
//            // Assert
//            _context.Connection.Key.Should().BeNullOrEmpty();
//            _context.Connection.Id.Should().BeNullOrEmpty();
//        }

        public RealtimeWorkflowSpecs(ITestOutputHelper output) : base(output)
        {
        }
    }
}