using System;
using IO.Ably.Platform;
using IO.Ably.Transport;

namespace AblyPlatform
{
    public class PlatformImpl : IPlatform
    {
        string IPlatform.GetConnectionString()
        {
            throw new NotSupportedException();
        }

        ICrypto IPlatform.GetCrypto()
        {
            return new Cryptography.CryptoImpl();
        }

        ITransportFactory IPlatform.GetWebSocketsFactory()
        {
            return new WebSocketTransport.WebSocketTransportFactory();
        }
    }
}