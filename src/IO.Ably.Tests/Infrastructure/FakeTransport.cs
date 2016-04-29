using System;
using IO.Ably.Transport;
using IO.Ably.Types;

namespace IO.Ably.Tests
{
    public class FakeTransport : ITransport
    {
        public TransportParams Parameters { get; }

        public FakeTransport(TransportState? state = null)
        {
            if (state.HasValue)
                State = state.Value;
        }

        public FakeTransport(TransportParams parameters)
        {
            Parameters = parameters;
        }

        public bool ConnectCalled { get; set; }

        public bool CloseCalled { get; set; }

        public bool AbortCalled { get; set; }

        public ProtocolMessage LastMessageSend { get; set; }
        public string Host { get; set; }
        public TransportState State { get; set; }
        public ITransportListener Listener { get; set; }

        public void Connect()
        {
            ConnectCalled = true;
            Listener?.OnTransportConnected();
            State = TransportState.Connected;
            
        }

        public void Close()
        {
            CloseCalled = true;
            Listener?.OnTransportMessageReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Closed));
            Listener?.OnTransportDisconnected();
        }


        public void Abort(string reason)
        {
            AbortCalled = true;
        }

        public Action<ProtocolMessage> SendAction = delegate { };

        public void Send(ProtocolMessage message)
        {
            SendAction(message);
            LastMessageSend = message;
        }
    }
}