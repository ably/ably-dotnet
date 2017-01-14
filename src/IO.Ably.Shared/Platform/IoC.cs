using IO.Ably.Realtime;
using IO.Ably.Transport;

namespace IO.Ably.Platform
{
    /// <summary>This class initializes dynamically-injected platform dependencies.</summary>
    public static class IoC
    {
        //public static ICrypto Crypto => new Crypto();

        public static ITransportFactory WebSockets => new WebSocketTransport.WebSocketTransportFactory();
    }
}