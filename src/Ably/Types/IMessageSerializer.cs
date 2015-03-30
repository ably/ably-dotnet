using System;

namespace Ably.Types
{
    public interface IMessageSerializer
    {
        object SerializeProtocolMessage(ProtocolMessage message);
        ProtocolMessage DeserializeProtocolMessage(object value);
    }
}
