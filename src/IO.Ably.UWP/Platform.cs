using IO.Ably.Transport;
using System.Net.NetworkInformation;
using Windows.ApplicationModel.UserDataTasks;
using Windows.Networking.Connectivity;
using Windows.UI.Core;
using IO.Ably.Realtime;

namespace IO.Ably
{
    internal class Platform : IPlatform
    {
        internal static bool _hookedUpToNetworkEvents = false;
        private static readonly object _lock = new object();

        public string PlatformId => "uwp";

        public ITransportFactory TransportFactory => null;

        public void RegisterOsNetworkStateChanged()
        {
            lock (_lock)
            {
                if (_hookedUpToNetworkEvents == false)
                {
                    NetworkInformation.NetworkStatusChanged += sender =>
                    {
                        ConnectionProfile InternetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
                        Connection.NotifyOperatingSystemNetworkState(
                            InternetConnectionProfile == null ? NetworkState.Offline : NetworkState.Online);
                    };
                }

                _hookedUpToNetworkEvents = true;
            }
        }
    }
}
