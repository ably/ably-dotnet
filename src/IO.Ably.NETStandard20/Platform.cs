using System.Net.NetworkInformation;
using IO.Ably.Push;
using IO.Ably.Realtime;
using IO.Ably.Transport;

namespace IO.Ably
{
    internal class Platform : IPlatform
    {
        private static readonly object Lock = new object();

        static Platform()
        {
            Initialize();
        }

        internal static bool HookedUpToNetworkEvents { get; private set; }

        // Defined as per https://learn.microsoft.com/en-us/dotnet/standard/frameworks#preprocessor-symbols
        // TODO : Get runtime platform info ( android, iOS ) using https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/device/information?view=net-maui-7.0&tabs=windows
        // TODO : Get all operating system from https://stackoverflow.com/questions/38790802/determine-operating-system-in-net-core
        // System.OperatingSystem.IsWindows(), System.OperatingSystem.IsIOS()
        // Also, update OS detection for dotnetcore
#if NET6_0
        public string PlatformId => "net6.0";
#elif NET7_0
        public string PlatformId => "net7.0";
#else
        public string PlatformId => "netstandard20";
#endif

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
