using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably;
using IO.Ably.Types;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests
{
    public class ProtocolMessageSpecs
    {
        public class WithMsgPackEnconding
        {
            [Fact]
            public void CanSerialiseAndDeserializeProtocolMessage()
            {
                var message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, "boo");
                message.Presence = new[] { new PresenceMessage(PresenceAction.Enter, "123", "my data") };

                var data = MsgPackHelper.Serialise(message);
                var result = MsgPackHelper.DeSerialise(data, typeof(ProtocolMessage)) as ProtocolMessage;

                result.Action.Should().Be(message.Action);
                result.Presence.First().Data.Should().Be(message.Presence[0].Data);
            }
        }

        public ProtocolMessageSpecs(ITestOutputHelper output)
        {
        }
    }
}
