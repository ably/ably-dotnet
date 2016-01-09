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

        ICrypto IPlatform.getCrypto()
        {
            return new Cryptography.CryptoImpl();
        }

        ITransportFactory IPlatform.getWebSocketsFactory()
        {
            return new WebSocketTransport.WebSocketTransportFactory();
        }
    }
}