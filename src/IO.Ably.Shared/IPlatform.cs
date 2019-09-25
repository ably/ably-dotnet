using IO.Ably.Transport;

namespace IO.Ably
{
    internal interface IPlatform
    {
        string PlatformId { get; }

        bool SyncContextDefault { get; }

        ITransportFactory TransportFactory { get; }
    }
}
