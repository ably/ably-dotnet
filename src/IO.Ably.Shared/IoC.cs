using System;
using System.Reflection;
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
            var name = new AssemblyName("IO.Ably");
            var type = Assembly.Load(name).GetType("IO.Ably.Platform");
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

        public static ITransportFactory TransportFactory => Platform?.TransportFactory ?? new MsWebSocketTransport.TransportFactory(); 
    }
}