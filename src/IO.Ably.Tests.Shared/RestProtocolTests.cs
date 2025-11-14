using FluentAssertions;
using Xunit;

namespace IO.Ably.Tests
{
    public class RestProtocolTests
    {
        [Fact]
        public void WhenProtocolIsNotDefined_DefaultsToMsgPack()
        {
            var rest = new AblyRest(new ClientOptions("API_KEY"));
            if (Defaults.MsgPackEnabled)
            {
                rest.Protocol.Should().Be(Protocol.MsgPack);
            }
            else
            {
#pragma warning disable CS0162 // Unreachable code detected
                rest.Protocol.Should().Be(Protocol.Json);
#pragma warning restore CS0162 // Unreachable code detected
            }
        }

        [Theory]
        [InlineData(true, Protocol.MsgPack)]
        [InlineData(false, Protocol.Json)]
        public void WhenUseBinaryProtocolIsSet_ProtocolIsSetCorrectly(bool useBinaryProtocol, Protocol expectedProtocol)
        {
            var rest = new AblyRest(new ClientOptions { UseBinaryProtocol = useBinaryProtocol, Key = "best.test:key" });
            rest.Protocol.Should().Be(expectedProtocol);
        }

        [Fact]
        public void WhenProtocolIsJson_RestProtocolIsSetToJson()
        {
            var rest = new AblyRest(new ClientOptions { UseBinaryProtocol = false, Key = "best.test:key" });
            rest.Protocol.Should().Be(Protocol.Json);
        }

        [Fact]
        public void WhenUseBinaryIsFalse_ProtocolIsSetToJson()
        {
            var rest = new AblyRest(new ClientOptions { UseBinaryProtocol = false, Key = "best.test:key" });
            rest.Protocol.Should().Be(Protocol.Json);
        }
    }
}
