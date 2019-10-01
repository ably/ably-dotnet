using IO.Ably.Transport;
using System;
using System.Net.NetworkInformation;
using IO.Ably.Realtime;
using Microsoft.Win32;

namespace IO.Ably
{
    internal class Platform : IPlatform
    {
        public string PlatformId => "framework";

        public ITransportFactory TransportFactory => null;

        static Platform()
        {
            NetworkChange.NetworkAvailabilityChanged += (sender, eventArgs) =>
                Connection.NotifyOperatingSystemNetworkState(eventArgs.IsAvailable ? NetworkState.Online : NetworkState.Offline);
        }
    }
}