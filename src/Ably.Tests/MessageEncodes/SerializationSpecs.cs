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

namespace Ably.Tests.MessageEncodes
{
    public class ProtocolMessageSpecs
    {
        public class WithMsgPackEnconding
        {
            [Fact]
            public void CanSerialiseAndDeserializeProtocolMessage()
            {
                var serializer = new MsgPackMessageSerializer();
                var message = new ProtocolMessage(ProtocolMessage.MessageAction.Presence, "boo");
                message.presence = new[] { new PresenceMessage(PresenceMessage.ActionType.Enter, "123", "my data") };

                var data = serializer.SerializeProtocolMessage(message);
                var result = serializer.DeserializeProtocolMessage(data);

                result.action.Should().Be(message.action);
                result.presence.First().data.Should().Be(message.presence[0].data);
            }
        }

        public ProtocolMessageSpecs(ITestOutputHelper output)
        {
        }
    }
}
