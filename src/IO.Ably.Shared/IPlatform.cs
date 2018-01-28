using IO.Ably.Transport;

namespace IO.Ably
{
    internal interface IPlatform
    {
        string PlatformId { get; }
        ITransportFactory TransportFactory { get; }
    }
}