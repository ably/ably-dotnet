using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using IO.Ably.Push;
using IO.Ably.Realtime;
using IO.Ably.Transport;

namespace IO.Ably
{
    internal class Platform : IPlatform
    {
        private static readonly object Lock = new object();
        private Agent.PlatformRuntime? _platformId;

        static Platform()
        {
            Initialize();
        }

        internal static bool HookedUpToNetworkEvents { get; private set; }

        // Use runtime detection via RuntimeInformation.FrameworkDescription
        // This detects the actual runtime version, not the compile-time target framework
        // This is important because netstandard2.0 assemblies can run on .NET 6/7/8/9+
        // and we want to report the actual runtime being used
        public Agent.PlatformRuntime PlatformId
        {
            get
            {
                if (_platformId.HasValue)
                {
                    return _platformId.Value;
                }

                // Default fallback for netstandard2.0 or unknown runtimes
                _platformId = Agent.PlatformRuntime.Netstandard20;

                try
                {
                    var frameworkDescription = RuntimeInformation.FrameworkDescription;

                    if (frameworkDescription.StartsWith(".NET 6.", System.StringComparison.OrdinalIgnoreCase))
                    {
                        _platformId = Agent.PlatformRuntime.Net6;
                    }

                    if (frameworkDescription.StartsWith(".NET 7.", System.StringComparison.OrdinalIgnoreCase))
                    {
                        _platformId = Agent.PlatformRuntime.Net7;
                    }
                }
                catch
                {
                    // fall back to Netstandard20
                }

                return _platformId.Value;
            }
        }

        public ITransportFactory TransportFactory => null;

        public IMobileDevice MobileDevice { get; set; }

        internal static void Initialize()
        {
            HookedUpToNetworkEvents = false;
        }

        public void RegisterOsNetworkStateChanged()
        {
            lock (Lock)
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
