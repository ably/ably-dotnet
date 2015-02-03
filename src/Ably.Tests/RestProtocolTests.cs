using FluentAssertions;
using Xunit;

namespace Ably.Tests
{
    public class RestProtocolTests
    {
        [Fact]
        public void WhenProtocolIsNotDefined_DefaultsToMsgPack()
        {
            var rest = new RestClient(new AblyOptions());
            rest.Protocol.Should().Be(Protocol.MsgPack);
        }

        [Fact]
        public void WhenProtocolIsJson_RestProtocolIsSetToJson()
        {
            var rest = new RestClient(new AblyOptions() { UseBinaryProtocol = false});
            rest.Protocol.Should().Be(Protocol.Json);
        }

        [Fact]
        public void WhenUseBinaryIsFalse_ProtocolIsSetToJson()
        {
            var rest = new RestClient(new AblyOptions() {UseBinaryProtocol = false});
            rest.Protocol.Should().Be(Protocol.Json);
        }

        [Fact]
        public void WhenProtocolIsMsgPack_ProtocolIsSetToMsgPack()
        {
            var rest = new RestClient(new AblyOptions() { UseBinaryProtocol = true});
            rest.Protocol.Should().Be(Protocol.MsgPack);
        }
    }
}