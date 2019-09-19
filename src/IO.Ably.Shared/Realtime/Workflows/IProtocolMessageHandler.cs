using System.Threading.Tasks;
using IO.Ably.Types;

namespace IO.Ably.Realtime.Workflows
{
    public interface IProtocolMessageHandler
    {
        ValueTask<bool> OnMessageReceived(ProtocolMessage message);
    }
}