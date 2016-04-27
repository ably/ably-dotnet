using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IO.Ably.Realtime;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;
using ConnectionState = IO.Ably.Transport.States.Connection.ConnectionState;

namespace IO.Ably.Tests
{
    internal class FakeTimer : ICountdownTimer
    {
        public void Start(TimeSpan delay, Action onTimeOut, bool autoReset = false)
        {
            StartedWithAction = true;
            LastDelay = delay;
            AutoRest = autoReset;
        }

        public bool AutoRest { get; set; }

        public TimeSpan LastDelay { get; set; }

        public bool StartedWithAction { get; set; }

        public void StartAsync(TimeSpan delay, Func<Task> onTimeOut, bool autoReset = false)
        {
            StartedWithFunc = true;
            LastDelay = delay;
            AutoRest = autoReset;
        }

        public bool StartedWithFunc { get; set; }

        public void Abort()
        {
            throw new NotImplementedException();
        }
    }

    internal class FakeTransport : ITransport
    {
        public string Host { get; set; }
        public TransportState State { get; set; }
        public ITransportListener Listener { get; set; }

        public void Connect()
        {
            ConnectCalled = true;
        }

        public bool ConnectCalled { get; set; }

        public void Close()
        {
            CloseCalled = true;
        }

        public bool CloseCalled { get; set; }

        public void Abort(string reason)
        {
            AbortCalled = true;
        }

        public bool AbortCalled { get; set; }

        public void Send(ProtocolMessage message)
        {
            LastMessageSend = message;
        }

        public ProtocolMessage LastMessageSend { get; set; }
    }

    internal class FakeConnectionContext : IConnectionContext
    {
        public ConnectionState State { get; set; }
        public ITransport Transport { get; set; }
        public AblyRest RestClient { get; set; }
        public Queue<ProtocolMessage> QueuedMessages { get; } = new Queue<ProtocolMessage>();
        public Connection Connection { get; set; }
        public DateTimeOffset? FirstConnectionAttempt { get; set; }
        public int ConnectionAttempts { get; set; }

        public ConnectionState LastSetState { get; set; }

        public FakeConnectionContext()
        {
            Connection = new Connection();
        }

        public void SetState(ConnectionState state)
        {
            State = state;
            LastSetState = state;
        }

        public bool CreateTransportCalled;
        public Task CreateTransport()
        {
            CreateTransportCalled = true;
            Transport = new FakeTransport();
            return TaskConstants.BooleanTrue;
        }

        public bool DestroyTransportCalled;
        public void DestroyTransport()
        {
            DestroyTransportCalled = true;
        }

        public bool AttempConnectionCalled;
        public void AttemptConnection()
        {
            AttempConnectionCalled = true;
        }

        public bool ResetConnectionAttemptsCalled;
        public void ResetConnectionAttempts()
        {
            ResetConnectionAttemptsCalled = true;
        }

        public bool CanConnectToAblyBool = true;
        public Task<bool> CanConnectToAbly()
        {
            return Task.FromResult(CanConnectToAblyBool);
        }
    }
}