using System;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
    internal interface IConnectionContext
    {
        TimeSpan DefaultTimeout { get; }

        TimeSpan RetryTimeout { get; }

        void SendToTransport(ProtocolMessage message);

        void ExecuteCommand(RealtimeCommand cmd);

        ITransport Transport { get; }

        Connection Connection { get; }

        TimeSpan SuspendRetryTimeout { get; }

        void ClearTokenAndRecordRetry();

        //Task SetState(ConnectionStateBase state, bool skipAttach = false);

        Task CreateTransport();

        void DestroyTransport(bool suppressClosedEvent = true);

        void SetConnectionClientId(string clientId);

        bool ShouldWeRenewToken(ErrorInfo error);

        void Send(ProtocolMessage message, Action<bool, ErrorInfo> callback = null, ChannelOptions channelOptions = null);

        Task RetryAuthentication(ErrorInfo error = null, bool updateState = true);

        void HandleConnectingFailure(ErrorInfo error, Exception ex);

        void SendPendingMessages(bool resumed);

        void ClearAckQueueAndFailMessages(ErrorInfo error);

        Task<bool> CanUseFallBackUrl(ErrorInfo error);

        void DetachAttachedChannels(ErrorInfo error);
    }
}
