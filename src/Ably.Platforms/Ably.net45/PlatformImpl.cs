using Ably.Platform;
using Ably.Transport;
using System.Configuration;

namespace Ably
{
    public class PlatformImpl : IPlatform
    {
        string IPlatform.getConnectionString()
        {
            var connString = ConfigurationManager.ConnectionStrings["Ably"];
            if( connString == null )
            {
                return string.Empty;
            }
            return connString.ConnectionString;
        }

        ICrypto IPlatform.crypto
        {
            get
            {
                return new Cryptography.CryptoImpl();
            }
        }

        ITransportFactory IPlatform.webSockets
        {
            get
            {
                return new WebSocketTransport.WebSocketTransportFactory();
            }
        }
    }
}