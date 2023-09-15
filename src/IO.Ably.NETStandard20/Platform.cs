using System.Net.NetworkInformation;
using IO.Ably.Realtime;

namespace IO.Ably
{
    internal class Platform : IPlatform
    {
        // Defined as per https://learn.microsoft.com/en-us/dotnet/standard/frameworks#preprocessor-symbols
#if NET6_0
        public Agent.PlatformRuntime PlatformId => Agent.PlatformRuntime.Net6;
#elif NET7_0
        public Agent.PlatformRuntime PlatformId => Agent.PlatformRuntime.Net7;
#else
        public Agent.PlatformRuntime PlatformId => Agent.PlatformRuntime.Netstandard20;
#endif

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
