using IO.Ably.Transport;

namespace IO.Ably
{
    class Platform : IPlatform
    {
        public ITransportFactory TransportFactory => new MsWebSocketTransport.TransportFactory();
    }
}
