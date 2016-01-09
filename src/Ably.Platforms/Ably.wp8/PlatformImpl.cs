using Ably.Platform;
using Ably.Transport;
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

        ICrypto IPlatform.crypto
        {
            get
            {
                return new Cryptography.CryptoImpl();
            }
        }

        class WebSocketTransportFactory : ITransportFactory
        {
            public ITransport CreateTransport( TransportParams parameters )
            {
                throw new NotImplementedException();
            }
        }

        ITransportFactory IPlatform.webSockets
        {
            get
            {
                return new WebSocketTransportFactory();
            }
        }
    }
}