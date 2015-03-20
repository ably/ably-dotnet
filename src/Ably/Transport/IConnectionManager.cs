using Ably.Realtime;
using Ably.Types;
using System;

namespace Ably.Transport
{
    public delegate void StateChangedDelegate(ConnectionState state);
    public delegate void MessageReceivedDelegate();

    public interface IConnectionManager
    {
        event StateChangedDelegate StateChanged;

        event MessageReceivedDelegate MessageReceived;

        bool IsActive { get; }

        void Connect();

        void Close();

        void Ping();

        void Send(ProtocolMessage message);
    }
}
