using System.Threading.Tasks;
using IO.Ably.Types;

namespace IO.Ably.Realtime.Workflow
{
    internal interface IProtocolMessageHandler
    {
        ValueTask<bool> OnMessageReceived(ProtocolMessage message, RealtimeState state);
    }
}