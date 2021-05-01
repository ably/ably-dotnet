using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Trait("spec", "RTN10")]
    public class ConnectionSerialSpecs : AblyRealtimeSpecs
    {
        [Fact]
        [Trait("spec", "RTN10a")]
        public async Task OnceConnected_ConnectionSerialShouldBeMinusOne()
        {
            var client = GetClientWithFakeTransport();
            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));
            await client.WaitForState(ConnectionState.Connected);

            client.Connection.Serial.Should().Be(-1);
        }

        [Fact]
        [Trait("spec", "RTN10c")]
        public async Task WhenRestoringConnection_UsesLastKnownConnectionSerial()
        {
            // Arrange
            var client = GetClientWithFakeTransport();
            long targetSerial = 1234567;
            client.State.Connection.Serial = targetSerial;

            // Act
            var transportParams = await client.ConnectionManager.CreateTransportParameters("https://realtime.ably.io");

            transportParams.ConnectionSerial.Should().Be(targetSerial);
        }

        [Fact]
        [Trait("spec", "RTN10b")]
        public async Task WhenProtocolMessageWithSerialReceived_SerialShouldUpdate()
        {
            // Arrange
            var client = GetClientWithFakeTransport();
            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

            await client.WaitForState(ConnectionState.Connected);
            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Message)
            {
                ConnectionSerial = 123456
            });

            await client.ProcessCommands();

            // Act
            client.Connection.Serial.Should().Be(123456);
        }

        [Fact]
        [Trait("spec", "RTN10b")]
        public void WhenProtocolMessageWithOUTSerialReceived_SerialShouldNotUpdate()
        {
            // Arrange
            var client = GetClientWithFakeTransport();
            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));
            var initialSerial = client.Connection.Serial;

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Message));

            // Act
            client.Connection.Serial.Should().Be(initialSerial);
        }

        public ConnectionSerialSpecs(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}
