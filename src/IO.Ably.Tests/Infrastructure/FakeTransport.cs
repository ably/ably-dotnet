using IO.Ably.Transport;
using IO.Ably.Types;

namespace IO.Ably.Tests
{
    internal class FakeTransport : ITransport
    {
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
        }

        public void Close()
        {
            CloseCalled = true;
        }

        public void Abort(string reason)
        {
            AbortCalled = true;
        }

        public void Send(ProtocolMessage message)
        {
            LastMessageSend = message;
        }
    }
}