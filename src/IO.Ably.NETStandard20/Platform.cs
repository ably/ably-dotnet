using System.Net.NetworkInformation;
using IO.Ably.Push;
using IO.Ably.Realtime;
using IO.Ably.Transport;

namespace IO.Ably
{
    internal class Platform : IPlatform
    {
        private static readonly object _lock = new object();

        static Platform()
        {
            Initialize();
        }

        internal static bool HookedUpToNetworkEvents { get; private set; }

        public string PlatformId => "netstandard20";

        public ITransportFactory TransportFactory => null;

        public IMobileDevice MobileDevice { get; set; }

        internal static void Initialize()
        {
            HookedUpToNetworkEvents = false;
        }

        public void RegisterOsNetworkStateChanged()
        {
            lock (_lock)
            {
                if (HookedUpToNetworkEvents == false)
                {
                    NetworkChange.NetworkAvailabilityChanged += (sender, eventArgs) =>
                        Connection.NotifyOperatingSystemNetworkState(eventArgs.IsAvailable ? NetworkState.Online : NetworkState.Offline);
                }

                HookedUpToNetworkEvents = true;
            }
        }
    }
}
