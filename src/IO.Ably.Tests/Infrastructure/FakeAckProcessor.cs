using System;
using IO.Ably.Transport;
using IO.Ably.Transport.States.Connection;
using IO.Ably.Types;

namespace IO.Ably.Tests
{
    internal class FakeAckProcessor : IAcknowledgementProcessor
    {
        public bool SendMessageCalled { get; set; }
        public bool OnMessageReceivedCalled { get; set; }
        public ConnectionState LastState { get; set; }
        public bool OnStatecChanged { get; set; }

        public void SendMessage(ProtocolMessage message, Action<bool, ErrorInfo> callback)
        {
            SendMessageCalled = true;
        }

        public bool OnMessageReceived(ProtocolMessage message)
        {
            OnMessageReceivedCalled = true;
            return true;
        }

        public void OnStateChanged(ConnectionState state)
        {
            OnStatecChanged = true;
            LastState = state;
        }
    }
}