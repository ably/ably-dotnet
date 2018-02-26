using System;
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

            if (message.Action != ProtocolMessage.MessageAction.Connected)
            {
                throw new InvalidOperationException("Can only create Connection info from Connected message. Current passed: " + message.Action);
            }

            ConnectionId = message.ConnectionId;
            ConnectionSerial = message.ConnectionSerial ?? -1;
            ConnectionKey = message.ConnectionKey;
            ClientId = message.ConnectionDetails?.ClientId;
            ConnectionStateTtl = message.ConnectionDetails?.ConnectionStateTtl;
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
        Task Execute(Action action);

        Connection Connection { get; }

        TimeSpan DefaultTimeout { get; }

        ClientOptions Options { get; }

        bool IsActive { get; }

        void Send(ProtocolMessage message, Action<bool, ErrorInfo> callback = null, ChannelOptions channelOptions = null);
        void FailMessageWaitingForAckAndClearOutgoingQueue(RealtimeChannel realtimeChannel, ErrorInfo error);
    }

    internal interface IConnectionContext
    {
        TimeSpan DefaultTimeout { get; }
        TimeSpan RetryTimeout { get; }
        void SendToTransport(ProtocolMessage message);
        Task Execute(Action action);
        ITransport Transport { get; }
        Connection Connection { get; }
        TimeSpan SuspendRetryTimeout { get; }
        void ClearTokenAndRecordRetry();
        Task SetState(States.Connection.ConnectionStateBase state, bool skipAttach = false);
        Task CreateTransport();
        void DestroyTransport(bool suppressClosedEvent = true);
        void SetConnectionClientId(string clientId);
        bool ShouldWeRenewToken(ErrorInfo error);
        void Send(ProtocolMessage message, Action<bool, ErrorInfo> callback = null, ChannelOptions channelOptions = null);
        Task<bool> RetryBecauseOfTokenError(ErrorInfo error);
        void HandleConnectingFailure(ErrorInfo error, Exception ex);
        void SendPendingMessages(bool resumed);
        void ClearAckQueueAndFailMessages(ErrorInfo error);
        Task<bool> CanUseFallBackUrl(ErrorInfo error);
        void DetachAttachedChannels(ErrorInfo error);
    }
}