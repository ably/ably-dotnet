using System;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
    public delegate void MessageReceivedDelegate(ProtocolMessage message);

    internal interface IConnectionManager
    {
        Task Execute(Action action);

        Connection Connection { get; }

        TimeSpan DefaultTimeout { get; }

        ClientOptions Options { get; }

        bool IsActive { get; }

        void Send(ProtocolMessage message, Action<bool, ErrorInfo> callback = null, ChannelOptions channelOptions = null);

        void FailMessageWaitingForAckAndClearOutgoingQueue(RealtimeChannel realtimeChannel, ErrorInfo error);
    }
}
