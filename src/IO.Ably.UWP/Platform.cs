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
        public string PlatformId => "uwp";

        public ITransportFactory TransportFactory => null;

        static Platform()
        {
            NetworkInformation.NetworkStatusChanged += sender =>
                {
                    ConnectionProfile InternetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
                    Connection.NotifyOperatingSystemNetworkState(
                        InternetConnectionProfile == null ? NetworkState.Offline : NetworkState.Online);
                };
        }
    }
}
