﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
    public class ConnectionInfo
    {
        public static readonly ConnectionInfo Empty = new ConnectionInfo();
        private ConnectionInfo() { }

        public ConnectionInfo(string connectionId, long connectionSerial, string connectionKey, string clientId, TimeSpan? connectionStateTtl = null)
        {
            ClientId = clientId;
            ConnectionId = connectionId;
            ConnectionSerial = connectionSerial;
            ConnectionKey = connectionKey;
            ConnectionStateTtl = connectionStateTtl;
        }

        public ConnectionInfo(ProtocolMessage message)
        {
            if(message == null)
                throw new ArgumentNullException(nameof(message), "Null message");

            if (message.action != ProtocolMessage.MessageAction.Connected)
            {
                throw new InvalidOperationException("Can only create Connection info from Connected message. Current passed: " + message.action);
            }

            ConnectionId = message.connectionId;
            ConnectionSerial = message.connectionSerial ?? -1;
            ConnectionKey = message.connectionKey;
            ClientId = message.connectionDetails?.clientId;
            ConnectionStateTtl = message.connectionDetails?.connectionStateTtl;
        }

        public TimeSpan? ConnectionStateTtl { get; private set; }
        public string ClientId { get; private set; }
        public string ConnectionId { get; private set; }
        public long ConnectionSerial { get; private set; }
        public string ConnectionKey { get; private set; }
    }

    public delegate void MessageReceivedDelegate(ProtocolMessage message);

    internal interface IConnectionManager
    {
        Connection Connection { get; }

        TimeSpan DefaultTimeout { get; }

        ClientOptions Options { get; }

        ConnectionStateType ConnectionState { get; }

        bool IsActive { get; }
        event MessageReceivedDelegate MessageReceived;
        
        void Send(ProtocolMessage message, Action<bool, ErrorInfo> callback);
    }

    internal interface IConnectionContext
    {
        TimeSpan DefaultTimeout { get; }
        TimeSpan RetryTimeout { get; }
        void SendToTransport(ProtocolMessage message);
        Task Execute(Action action);
        TransportState TransportState { get; }
        ITransport Transport { get; }
        Connection Connection { get; }
        TimeSpan SuspendRetryTimeout { get; }
        void ClearTokenAndRecordRetry();
        Task SetState(States.Connection.ConnectionState state, bool skipAttach = false);
        Task CreateTransport();
        void DestroyTransport(bool suppressClosedEvent = true);
        void SetConnectionClientId(string clientId);
        bool ShouldWeRenewToken(ErrorInfo error);
        void Send(ProtocolMessage message, Action<bool, ErrorInfo> callback = null);
        bool ShouldSuspend();
        Task<bool> RetryBecauseOfTokenError(ErrorInfo error);
        void HandleConnectingFailure(ErrorInfo error, Exception ex);
        void SendPendingMessages(bool resumed);
        void ClearAckQueueAndFailMessages(ErrorInfo error);
        Task<bool> CanUseFallBackUrl(ErrorInfo error);
    }
}