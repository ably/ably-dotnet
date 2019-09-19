using System.Threading.Tasks;
using IO.Ably.Types;

namespace IO.Ably.Realtime.Workflow
{
    public interface IProtocolMessageHandler
    {
        ValueTask<bool> OnMessageReceived(ProtocolMessage message);
    }
}