﻿using System.Net.NetworkInformation;
using IO.Ably.Realtime;
using IO.Ably.Transport;

namespace IO.Ably
{
    internal class Platform : IPlatform
    {
        internal static bool _hookedUpToNetworkEvents = false;
        private static readonly object _lock = new object();

        public string PlatformId => "netstandard20";

        public ITransportFactory TransportFactory => null;

        public void RegisterOsNetworkStateChanged()
        {
            lock (_lock)
            {
                if (_hookedUpToNetworkEvents == false)
                {
                    NetworkChange.NetworkAvailabilityChanged += (sender, eventArgs) =>
                        Connection.NotifyOperatingSystemNetworkState(eventArgs.IsAvailable ? NetworkState.Online : NetworkState.Offline);
                }

                _hookedUpToNetworkEvents = true;
            }
        }
    }
}
