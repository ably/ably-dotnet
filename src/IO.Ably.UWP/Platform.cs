using IO.Ably.Transport;
using System.Net.NetworkInformation;
using Windows.ApplicationModel.UserDataTasks;
using Windows.Networking.Connectivity;
using Windows.UI.Core;
using IO.Ably.Push;
using IO.Ably.Realtime;

namespace IO.Ably
{
    internal class Platform : IPlatform
    {
        private static readonly object _lock = new object();

        internal static bool HookedUpToNetworkEvents { get; private set; }

        public string PlatformId => "uwp";

        public ITransportFactory TransportFactory => null;

        public IMobileDevice MobileDevice { get; set; }

        public void RegisterOsNetworkStateChanged()
        {
            lock (_lock)
            {
                if (HookedUpToNetworkEvents == false)
                {
                    NetworkInformation.NetworkStatusChanged += sender =>
                    {
                        ConnectionProfile InternetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
                        Connection.NotifyOperatingSystemNetworkState(
                            InternetConnectionProfile == null ? NetworkState.Offline : NetworkState.Online);
                    };
                }

                HookedUpToNetworkEvents = true;
            }
        }
    }
}
