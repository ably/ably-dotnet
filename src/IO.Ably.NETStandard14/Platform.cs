using IO.Ably.Transport;

namespace IO.Ably
{
    internal class Platform : IPlatform
    {
        public string PlatformId => "netstandard14";
        public bool SyncContextDefault => false;
        public ITransportFactory TransportFactory => null;
    }
}