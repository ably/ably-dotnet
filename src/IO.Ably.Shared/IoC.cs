using System;
using System.IO;
using System.Reflection;
using IO.Ably.Push;
using IO.Ably.Transport;

namespace IO.Ably
{
    /// <summary>This class initializes dynamically-injected platform dependencies.</summary>
    internal static class IoC
    {
        private static readonly IPlatform Platform;

        /// <summary>Load AblyPlatform.dll, instantiate AblyPlatform.PlatformImpl type.</summary>
        static IoC()
        {
            try
            {
                var name = new AssemblyName("IO.Ably");
                var asm = Assembly.Load(name);
                var type = asm.GetType("IO.Ably.Platform");
                if (type != null)
                {
                    var obj = Activator.CreateInstance(type);
                    Platform = obj as IPlatform;
                }
                else
                {
                    DefaultLogger.Debug("Platform class does not exist. Defaulting Microsoft Websocket library.");
                }
            }
            catch (FileNotFoundException e)
            {
                DefaultLogger.Debug($"Assembly cannot be loaded. Defaulting Microsoft Websocket library. ({e.Message})");
            }
        }

        public static ITransportFactory TransportFactory => Platform?.TransportFactory ?? new MsWebSocketTransport.TransportFactory();

        public static void RegisterOsNetworkStateChanged() => Platform.RegisterOsNetworkStateChanged();

        public static string PlatformId => Platform?.PlatformId ?? string.Empty;

        public static IMobileDevice MobileDevice
        {
            get
            {
                try
                {
                    return Platform.MobileDevice;
                }
                catch (Exception e) when (e is NotImplementedException)
                {
                    DefaultLogger.Error("Mobile Device is no supported on the current platform.", e);
                    return null;
                }
            }
            set => Platform.MobileDevice = value;
        }
    }
}
