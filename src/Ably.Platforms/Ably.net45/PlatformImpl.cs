using Ably.Platform;
using Ably.Transport;
using System.Configuration;

namespace AblyPlatform
{
    public class PlatformImpl : IPlatform
    {
        public PlatformImpl() { }

        string IPlatform.getConnectionString()
        {
            var connString = ConfigurationManager.ConnectionStrings[ "Ably" ];
            if( connString == null )
            {
                return string.Empty;
            }
            return connString.ConnectionString;
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