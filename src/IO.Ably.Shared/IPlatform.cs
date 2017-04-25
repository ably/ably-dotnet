using IO.Ably.Transport;

namespace IO.Ably
{
    internal interface IPlatform
    {
        ITransportFactory TransportFactory { get; }
    }
}