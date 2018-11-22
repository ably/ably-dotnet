using System;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Types;

namespace IO.Ably.Transport
{
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

        Task RetryAuthentication();

        void HandleConnectingFailure(ErrorInfo error, Exception ex);

        void SendPendingMessages(bool resumed);

        void ClearAckQueueAndFailMessages(ErrorInfo error);

        Task<bool> CanUseFallBackUrl(ErrorInfo error);

        void DetachAttachedChannels(ErrorInfo error);
    }
}
