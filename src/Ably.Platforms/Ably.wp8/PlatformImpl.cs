using IO.Ably.Platform;
using IO.Ably.Transport;
using System;

namespace AblyPlatform
{
    public class PlatformImpl : IPlatform
    {
        public PlatformImpl() { }

        string IPlatform.getConnectionString()
        {
            throw new NotSupportedException();
        }

        ICrypto IPlatform.getCrypto()
        {
            return new Cryptography.CryptoImpl();
        }

        class WebSocketTransportFactory : ITransportFactory
        {
            public ITransport CreateTransport( TransportParams parameters )
            {
                throw new NotImplementedException();
            }
        }

        ITransportFactory IPlatform.getWebSocketsFactory()
        {
            return new WebSocketTransportFactory();
        }
    }
}