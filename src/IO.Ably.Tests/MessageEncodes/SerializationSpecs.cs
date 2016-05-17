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
                message.presence = new[] { new PresenceMessage(PresenceMessage.ActionType.Enter, "123", "my data") };

                var data = MsgPackHelper.Serialise(message);
                var result = MsgPackHelper.DeSerialise(data, typeof(ProtocolMessage)) as ProtocolMessage;

                result.action.Should().Be(message.action);
                result.presence.First().data.Should().Be(message.presence[0].data);
            }
        }

        public ProtocolMessageSpecs(ITestOutputHelper output)
        {
        }
    }
}
