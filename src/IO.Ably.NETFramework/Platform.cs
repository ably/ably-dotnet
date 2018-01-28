using IO.Ably.Transport;

namespace IO.Ably
{
    internal class Platform : IPlatform
    {
        public string PlatformId => "framework";
        public ITransportFactory TransportFactory => null;
    }
}