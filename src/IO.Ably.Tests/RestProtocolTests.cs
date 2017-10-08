using FluentAssertions;
using Xunit;

namespace IO.Ably.Tests
{
    public class RestProtocolTests
    {
        [Fact]
        public void WhenProtocolIsNotDefined_DefaultsToMsgPack()
        {
            if (!Config.MsgPackEnabled)
                return;

            var rest = new AblyRest(new ClientOptions());
            rest.Protocol.Should().Be(Protocol.MsgPack);
        }

        [Fact]
        public void WhenProtocolIsNotDefined_AndMsgPackInNotEnabled_DefaultsToJson()
        {
            if (Config.MsgPackEnabled)
                return;

            var rest = new AblyRest(new ClientOptions());
            rest.Protocol.Should().Be(Protocol.Json);
        }

        [Fact]
        public void WhenProtocolIsJson_RestProtocolIsSetToJson()
        {
            var rest = new AblyRest(new ClientOptions() { UseBinaryProtocol = false});
            rest.Protocol.Should().Be(Protocol.Json);
        }

        [Fact]
        public void WhenUseBinaryIsFalse_ProtocolIsSetToJson()
        {
            var rest = new AblyRest(new ClientOptions() {UseBinaryProtocol = false});
            rest.Protocol.Should().Be(Protocol.Json);
        }

        [Fact]
        public void WhenMsgPackIsDisabled_AndUseBinaryIsTrue_ProtocolIsSetToJson()
        {
            if (Config.MsgPackEnabled)
                return;

            var rest = new AblyRest(new ClientOptions() { UseBinaryProtocol = true });
            rest.Protocol.Should().Be(Protocol.Json);
        }

        [Fact]
        public void WhenProtocolIsMsgPack_ProtocolIsSetToMsgPack()
        {
            if (!Config.MsgPackEnabled)
                return;

            var rest = new AblyRest(new ClientOptions() { UseBinaryProtocol = true});
            rest.Protocol.Should().Be(Protocol.MsgPack);
        }
    }
}