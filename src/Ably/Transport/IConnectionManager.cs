﻿using Ably.Realtime;
using Ably.Types;
using System;

namespace Ably.Transport
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

    public interface IConnectionManager
    {
        event StateChangedDelegate StateChanged;

        event MessageReceivedDelegate MessageReceived;

        bool IsActive { get; }

        void Connect();

        void Close();

        void Send(ProtocolMessage message, Action<ErrorInfo> listener);
    }
}
