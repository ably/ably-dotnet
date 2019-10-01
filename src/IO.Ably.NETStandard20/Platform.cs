using IO.Ably.Transport;
using System.Net.NetworkInformation;
using IO.Ably.Realtime;

namespace IO.Ably
{
    internal class Platform : IPlatform
    {
        public string PlatformId => "netstandard20";

        public ITransportFactory TransportFactory => null;

        static Platform()
        {
            NetworkChange.NetworkAvailabilityChanged += (sender, eventArgs) =>
                Connection.NotifyOperatingSystemNetworkState(eventArgs.IsAvailable ? NetworkState.Online : NetworkState.Offline);
        }
    }
}
