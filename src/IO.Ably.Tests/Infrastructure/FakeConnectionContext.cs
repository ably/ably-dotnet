using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using IO.Ably.Realtime;
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
            Connection = new Connection();
        }

        public ConnectionState LastSetState { get; set; }
        public ConnectionState State { get; set; }
        public ITransport Transport { get; set; }
        public AblyRest RestClient { get; set; }
        public Queue<ProtocolMessage> QueuedMessages { get; } = new Queue<ProtocolMessage>();
        public Connection Connection { get; set; }
        public DateTimeOffset? FirstConnectionAttempt { get; set; }
        public int ConnectionAttempts { get; set; }

        public Task SetState(ConnectionState state)
        {
            State = state;
            LastSetState = state;
            return TaskConstants.BooleanTrue;
        }

        public Task CreateTransport()
        {
            CreateTransportCalled = true;
            Transport = new FakeTransport();
            return TaskConstants.BooleanTrue;
        }

        public T StateShouldBe<T>() where T : ConnectionState
        {
            LastSetState.Should().BeOfType<T>();
            return (T) LastSetState;
        } 

        public void ShouldHaveNotChangedState()
        {
            LastSetState.Should().BeNull();
        }

        public void DestroyTransport()
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
    }
}