using Ably.Transport;

namespace Ably.Platform
{
    public interface IPlatform
    {
        string getConnectionString();

        ICrypto getCrypto();

        ITransportFactory getWebSocketsFactory();
    }
}