using System.Net.NetworkInformation;
using IO.Ably.Realtime;

namespace IO.Ably
{
    internal class Platform : IPlatform
    {
        public Agent.PlatformRuntime PlatformId => Agent.PlatformRuntime.Framework;

        private static readonly object Lock = new object();

        internal static bool HookedUpToNetworkEvents { get; set; }

        public void RegisterOsNetworkStateChanged(ILogger logger)
        {
            lock (Lock)
            {
                if (HookedUpToNetworkEvents == false)
                {
                    NetworkChange.NetworkAvailabilityChanged += (sender, eventArgs) =>
                        Connection.NotifyOperatingSystemNetworkState(eventArgs.IsAvailable ? NetworkState.Online : NetworkState.Offline, logger);
                }

                HookedUpToNetworkEvents = true;
            }
        }
    }
}
