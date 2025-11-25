using FluentAssertions;
using System.Linq;
using Xunit;
using IO.Ably.Types;

namespace IO.Ably.Tests
{
    public class ProtocolMessageSpecs
    {
        [Fact]
        public void WithMsgPackEncoding_CanSerialiseAndDeserializeProtocolMessage()
        {
            // MsgPack is always enabled now
            var message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, "boo");
            message.Presence = new[] { new PresenceMessage(PresenceAction.Enter, "123", "my data") };

            var data = MsgPackHelper.Serialise<ProtocolMessage>(message);
            var result = MsgPackHelper.Deserialise<ProtocolMessage>(data);

            result.Action.Should().Be(message.Action);
            result.Presence.First().Data.Should().Be(message.Presence[0].Data);
        }
    }
}
