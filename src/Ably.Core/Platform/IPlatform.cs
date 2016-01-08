using Ably.Transport;

namespace Ably.Platform
{
    public interface IPlatform
    {
        string getConnectionString();

        ICrypto crypto { get; }

        ITransportFactory webSockets { get; }
    }
}