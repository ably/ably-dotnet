using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Trait("spec", "RTN10")]
    public class ConnectionSerialSpecs : ConnectionSpecsBase
    {
        [Fact]
        [Trait("spec", "RTN10a")]
        public void OnceConnected_ConnectionSerialShouldBeMinusOne()
        {
            var client = GetClientWithFakeTransport();
            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));
            client.Connection.Serial.Should().Be(-1);
        }

        [Fact]
        [Trait("spec", "RTN10c")]
        public async Task WhenRestoringConnection_UsesLastKnownConnectionSerial()
        {
            // Arrange
            var client = GetClientWithFakeTransport();
            long targetSerial = 1234567;
            client.Connection.Serial = targetSerial;

            // Act
            var transportParams = await client.ConnectionManager.CreateTransportParameters();

            transportParams.ConnectionSerial.Should().Be(targetSerial);
        }

        [Fact]
        [Trait("spec", "RTN10b")]
        public void WhenProtocolMessageWithSerialReceived_SerialShouldUpdate()
        {
            // Arrange
            var client = GetClientWithFakeTransport();
            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Connected));

            client.FakeProtocolMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Message)
            {
                ConnectionSerial = 123456
            });

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

        [Fact(Skip = "Need to get back to it")]
        [Trait("spec", "RTN10b")]
        public void WhenFirstAckMessageReceived_ShouldSetSerialToZero()
        {
        }

        public ConnectionSerialSpecs(ITestOutputHelper output)
            : base(output)
        {
        }
    }
}