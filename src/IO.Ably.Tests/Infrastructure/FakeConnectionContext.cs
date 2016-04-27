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

        public void SetState(ConnectionState state)
        {
            State = state;
            LastSetState = state;
        }

        public bool CreateTransportCalled;
        public Task CreateTransport()
        {
            CreateTransportCalled = true;
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
    }
}