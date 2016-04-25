using System;
using System.Collections.Generic;
using IO.Ably.Realtime;
using IO.Ably.Types;
using System.Threading.Tasks;

namespace IO.Ably.Transport
{
    public class ConnectionInfo
    {
        public ConnectionInfo(string connectionId, long connectionSerial, string connectionKey)
        {
            this.ConnectionId = connectionId;
            this.ConnectionSerial = connectionSerial;
            this.ConnectionKey = connectionKey;
        }

        public string ConnectionId { get; private set; }
        public long ConnectionSerial { get; private set; }
        public string ConnectionKey { get; private set; }
    }

    public delegate void StateChangedDelegate(ConnectionState state, ConnectionInfo info, ErrorInfo error);
    public delegate void MessageReceivedDelegate(ProtocolMessage message);

    internal interface IConnectionManager
    {
        event MessageReceivedDelegate MessageReceived;

        Connection Connection { get; }

        Realtime.ConnectionState ConnectionState { get; }

        bool IsActive { get; }

        void Connect();

        void Close();

        Task Ping();

        void Send( ProtocolMessage message, Action<bool, ErrorInfo> callback );
        Task SendAsync( ProtocolMessage message );
    }

    internal interface IConnectionContext
    {
        States.Connection.ConnectionState State { get; }
        ITransport Transport { get; }
        Queue<ProtocolMessage> QueuedMessages { get; }
        Connection Connection { get; }
        DateTimeOffset? FirstConnectionAttempt { get; }
        int ConnectionAttempts { get; }
        void SetState(States.Connection.ConnectionState state);
        void CreateTransport(bool useFallbackHost);
        void DestroyTransport();
        void AttemptConnection();
        void ResetConnectionAttempts();
    }
}
