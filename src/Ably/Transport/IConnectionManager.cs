using Ably.Realtime;
using Ably.Types;
using System;

namespace Ably.Transport
{
    public class ConnectionInfo
    {
        public string ConnectionId { get; set; }
        public long ConnectionSerial { get; set; }
        public string ConnectionKey { get; set; }
    }

    public delegate void StateChangedDelegate(ConnectionState state, ConnectionInfo info);
    public delegate void MessageReceivedDelegate(ProtocolMessage message);

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
