using IO.Ably.Transport;

namespace IO.Ably
{
    internal class Platform : IPlatform
    {
        public ITransportFactory TransportFactory => new MsWebSocketTransport.WebSocketTransportFactory();
    }
}
