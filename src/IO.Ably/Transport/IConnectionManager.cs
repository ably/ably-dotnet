using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
    public class ConnectionInfo
    {
        public ConnectionInfo(string connectionId, long connectionSerial, string connectionKey, string clientId)
        {
            ClientId = clientId;
            ConnectionId = connectionId;
            ConnectionSerial = connectionSerial;
            ConnectionKey = connectionKey;
        }

        public string ClientId { get; set; }
        public string ConnectionId { get; private set; }
        public long ConnectionSerial { get; private set; }
        public string ConnectionKey { get; private set; }
    }

    public delegate void StateChangedDelegate(ConnectionStateType state, ConnectionInfo info, ErrorInfo error);

    public delegate void MessageReceivedDelegate(ProtocolMessage message);

    internal interface IConnectionManager
    {
        Connection Connection { get; }

        ConnectionStateType ConnectionState { get; }

        bool IsActive { get; }
        event MessageReceivedDelegate MessageReceived;

        void Connect();

        void Close();

        Task Ping();

        void Send(ProtocolMessage message, Action<bool, ErrorInfo> callback);
        Task SendAsync(ProtocolMessage message);
    }

    internal interface IConnectionContext
    {
        States.Connection.ConnectionState State { get; }
        ITransport Transport { get; }
        AblyRest RestClient { get; }
        Queue<ProtocolMessage> QueuedMessages { get; }
        Connection Connection { get; }
        DateTimeOffset? FirstConnectionAttempt { get; }
        int ConnectionAttempts { get; }
        void SetState(States.Connection.ConnectionState state);
        Task CreateTransport();
        void DestroyTransport();
        void AttemptConnection();
        void ResetConnectionAttempts();
        Task<bool> CanConnectToAbly();
        void SetConnectionClientId(string clientId);
    }
}