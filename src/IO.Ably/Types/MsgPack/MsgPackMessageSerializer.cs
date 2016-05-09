using System;
using System.Linq;
using System.Net;
using IO.Ably.Types.MsgPack;
using MsgPack;

namespace IO.Ably.Types
{
    public class MsgPackMessageSerializer : IMessageSerializer
    {
        public ProtocolMessage DeserializeProtocolMessage(object value)
        {
            return (ProtocolMessage) MsgPackHelper.DeSerialise(value as byte[], typeof(ProtocolMessage));
        }

        public object SerializeProtocolMessage(ProtocolMessage message)
        {
            return MsgPackHelper.Serialise(message);
        }
    }
}