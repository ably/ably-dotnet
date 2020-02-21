using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
using IO.Ably.Realtime.Workflow;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;

namespace IO.Ably.Tests
{
    internal class FakeConnectionContext : IConnectionContext
    {
        private bool _attempConnectionCalled;

        public bool CanConnectToAblyBool { get; } = true;

        public bool CreateTransportCalled { get; private set; }

        public bool DestroyTransportCalled { get; private set; }

        public bool ResetConnectionAttemptsCalled { get; private set; }

        public FakeConnectionContext()
        {
            Connection = new Connection(null, TestHelpers.NowFunc());
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

        public void ExecuteCommand(RealtimeCommand cmd)
        {
            ExecutedCommands.Add(cmd);
        }

        public bool SendToTransportCalled { get; set; }

        public ConnectionStateBase State { get; set; }

        public List<RealtimeCommand> ExecutedCommands = new List<RealtimeCommand>();

        public ITransport Transport { get; set; }

        public Connection Connection { get; set; }

        public TimeSpan SuspendRetryTimeout { get; set; }

        private bool TriedToRenewToken { get; set; }

        public bool AllowTransportCreating { get; set; }

        public Task CreateTransport()
        {
            CreateTransportCalled = true;
            if (AllowTransportCreating)
            {
                Transport = new FakeTransport();
            }

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

        public bool ShouldWeRenewToken(ErrorInfo error, RealtimeState state)
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

        public void SendPendingMessages(bool resumed)
        {
            SendPendingMessagesCalled = true;
        }

        public void ClearAckQueueAndFailMessages(ErrorInfo error)
        {
            ClearAckQueueMessagesCalled = true;
            ClearAckMessagesError = error;
        }

        public void DetachAttachedChannels(ErrorInfo error)
        {
            DetachAttachedChannelsCalled = true;
        }

        public bool DetachAttachedChannelsCalled { get; set; }

        public ErrorInfo ClearAckMessagesError { get; set; }

        public bool ClearAckQueueMessagesCalled { get; set; }

        public bool SendPendingMessagesCalled { get; set; }

        public bool HandledConnectionFailureCalled { get; set; }

        public bool CloseConnectionCalled { get; set; }

        public bool ShouldSuspendValue { get; set; }

        public bool CanUseFallBack { get; set; }

        public bool AttempConnectionCalled { get => _attempConnectionCalled; set => _attempConnectionCalled = value; }

        public T StateShouldBe<T>() where T : ConnectionStateBase
        {
            LastSetState.Should().BeOfType<T>();
            return (T)LastSetState;
        }

        public void ShouldQueueCommand<T>(Action<T> commandCheck = null)
            where T : RealtimeCommand
        {
            var lastCommand = ExecutedCommands.LastOrDefault();
            lastCommand.Should().NotBeNull();
            lastCommand.Should().BeOfType<T>();
            commandCheck?.Invoke(lastCommand as T);
        }

        public void ShouldHaveNotChangedState()
        {
            LastSetState.Should().BeNull();
        }
    }
}
