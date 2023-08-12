using System;
using IO.Ably.Push;
using IO.Ably.Transport;

namespace IO.Ably
{
    /// <summary>This class initializes Platform.</summary>
    internal class IoC
    {
        public static readonly Platform Platform = new Platform();

        public static ITransportFactory TransportFactory => Platform?.TransportFactory ?? new MsWebSocketTransport.TransportFactory();

        public static Agent.PlatformRuntime PlatformId => Platform?.PlatformId ?? Agent.PlatformRuntime.Other;

        public IoC(ILogger logger)
        {
            Logger = logger;
        }

        public ILogger Logger { get; set; }

        public void RegisterOsNetworkStateChanged() => Platform.RegisterOsNetworkStateChanged(Logger);

        public IMobileDevice MobileDevice
        {
            get
            {
                try
                {
                    return Platform.MobileDevice;
                }
                catch (Exception e) when (e is NotImplementedException)
                {
                    Logger.Error("Mobile Device is no supported on the current platform.", e);
                    return null;
                }
            }
        }
    }
}
