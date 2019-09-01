using System;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
    public delegate void MessageReceivedDelegate(ProtocolMessage message);

    internal interface IConnectionManager
    {
        Connection Connection { get; }

        ClientOptions Options { get; }

        void Send(ProtocolMessage message, Action<bool, ErrorInfo> callback = null, ChannelOptions channelOptions = null);
    }
}
