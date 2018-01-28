using IO.Ably.Transport;

namespace IO.Ably
{
    internal class Platform : IPlatform
    {
        public string PlatformId => "uwp";
        public ITransportFactory TransportFactory => null;
    }
}