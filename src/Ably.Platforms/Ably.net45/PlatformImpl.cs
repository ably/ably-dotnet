using IO.Ably.Platform;
using IO.Ably.Transport;
using System.Configuration;

namespace AblyPlatform
{
    public class PlatformImpl : IPlatform
    {
        public PlatformImpl() { }

        string IPlatform.GetConnectionString()
        {
            var connString = ConfigurationManager.ConnectionStrings[ "Ably" ];
            if( connString == null )
            {
                return string.Empty;
            }
            return connString.ConnectionString;
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