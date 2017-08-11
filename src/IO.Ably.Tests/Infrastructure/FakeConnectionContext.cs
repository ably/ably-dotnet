using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Rest;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;

namespace IO.Ably.Tests
{
    internal class FakeConnectionContext : IConnectionContext
    {
        public bool AttempConnectionCalled;

        public bool CanConnectToAblyBool = true;

        public bool CreateTransportCalled;

        public bool DestroyTransportCalled;

        public bool ResetConnectionAttemptsCalled;

        public FakeConnectionContext()
        {
            Connection = new Connection(null);
        }

        public ConnectionStateBase LastSetState { get; set; }
        public IAblyAuth Auth { get; set; }

        public bool RenewTokenValue { get; set; }

        public bool ShouldWeRenewTokenValue { get; set; }

        public TimeSpan DefaultTimeout { get; set; } = Defaults.DefaultRealtimeTimeout;
        public TimeSpan RetryTimeout { get; set; } = Defaults.DisconnectedRetryTimeout;

        public void SendToTransport(ProtocolMessage message)
        {
            LastMessageSent = message;
            SendToTransportCalled = true;
        }

        public bool SendToTransportCalled { get; set; }

        public Task Execute(Action action)
        {
            action();
            return TaskConstants.BooleanTrue;
        }

        public ConnectionStateBase State { get; set; }
        public TransportState TransportState => Transport.State;
        public ITransport Transport { get; set; }
        public AblyRest RestClient { get; set; }
        public Queue<ProtocolMessage> QueuedMessages { get; } = new Queue<ProtocolMessage>();
        public Connection Connection { get; set; }
        public TimeSpan SuspendRetryTimeout { get; set; }

        public void ClearTokenAndRecordRetry()
        {
            TriedToRenewToken = true;
        }

        public bool TriedToRenewToken { get; set; }

        public Task SetState(ConnectionStateBase state, bool skipAttach)
        {
            State = state;
            LastSetState = state;
            return TaskConstants.BooleanTrue;
        }

        public bool AllowTransportCreating { get; set; }

        public Task CreateTransport()
        {
            CreateTransportCalled = true;
            if(AllowTransportCreating)
                Transport = new FakeTransport();
            return TaskConstants.BooleanTrue;
        }

        public void DestroyTransport(bool suppressClosedEvent = true)
        {
            DestroyTransportCalled = true;

            

        }

        public void AttemptConnection()
        {
            AttempConnectionCalled = true;
        }

        public void ResetConnectionAttempts()
        {
            ResetConnectionAttemptsCalled = true;
        }

        public Task<bool> CanConnectToAbly()
        {
            return Task.FromResult(CanConnectToAblyBool);
        }

        public void SetConnectionClientId(string clientId)
        {
        }

        public bool ShouldWeRenewToken(ErrorInfo error)
        {
            return ShouldWeRenewTokenValue;
        }

        public void Send(ProtocolMessage message, Action<bool, ErrorInfo> callback = null, ChannelOptions channelOptions = null)
        {
            LastMessageSent = message;
            LastCallback = callback;
            LastChannelOptions = channelOptions;
        }

        public ChannelOptions LastChannelOptions { get; set; }

        public Action<bool, ErrorInfo> LastCallback { get; set; }

        public ProtocolMessage LastMessageSent { get; set; }

        public bool ShouldSuspend()
        {
            return ShouldSuspendValue;
        }

        public Func<ErrorInfo, Task<bool>> RetryFunc = delegate { return TaskConstants.BooleanFalse; };

        public Task<bool> RetryBecauseOfTokenError(ErrorInfo error)
        {
            return RetryFunc(error);
        }

        public void CloseConnection()
        {
            CloseConnectionCalled = true;
        }

        public void HandleConnectingFailure(ErrorInfo error, Exception ex)
        {
            HandledConnectionFailureCalled = true;
        }

        public void SendPendingMessages(bool resumed)
        {
            SendPendingMessagesCalled = true;
        }

        public void ClearAckQueueAndFailMessages(ErrorInfo error)
        {
            ClearAckQueueMessagesCalled = true;
            ClearAckMessagesError = error;
        }

        public Task<bool> CanUseFallBackUrl(ErrorInfo error)
        {
            CanUseFallBackUrlCalled = true;
            return CanUseFallBack ? TaskConstants.BooleanTrue : TaskConstants.BooleanFalse;
        }

        public void DetachAttachedChannels(ErrorInfo error)
        {
            DetachAttachedChannelsCalled = true;
        }

        public bool DetachAttachedChannelsCalled { get; set; }

        public bool CanUseFallBackUrlCalled { get; set; }

        public ErrorInfo ClearAckMessagesError { get; set; }

        public bool ClearAckQueueMessagesCalled { get; set; }

        public bool SendPendingMessagesCalled { get; set; }

        public bool HandledConnectionFailureCalled { get; set; }

        public bool CloseConnectionCalled { get; set; }

        public bool ShouldSuspendValue { get; set; }
        public bool CanUseFallBack { get; set; }

        public T StateShouldBe<T>() where T : ConnectionStateBase
        {
            LastSetState.Should().BeOfType<T>();
            return (T) LastSetState;
        }

        public void ShouldHaveNotChangedState()
        {
            LastSetState.Should().BeNull();
        }
    }
}