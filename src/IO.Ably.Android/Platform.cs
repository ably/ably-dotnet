using IO.Ably.Transport;

namespace IO.Ably
{
    internal class Platform : IPlatform
    {
        public string PlatformId => "xamarin-android";
        public ITransportFactory TransportFactory => null;
    }
}