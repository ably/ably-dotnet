using System;

namespace IO.Ably.Types
{
    public interface IMessageSerializer
    {
        object SerializeProtocolMessage(ProtocolMessage message);
        ProtocolMessage DeserializeProtocolMessage(object value);
    }
}
