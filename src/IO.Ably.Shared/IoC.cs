﻿using System;
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
            try
            {
                var name = new AssemblyName("IO.Ably");
                var asm = Assembly.Load(name);
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
            catch (System.IO.FileNotFoundException e)
            {
                DefaultLogger.Debug($"Assembly cannot be loaded. Defaulting Microsoft Websocket library. ({e.Message})");
            }
        }

        public static ITransportFactory TransportFactory => Platform?.TransportFactory ?? new MsWebSocketTransport.TransportFactory();

        public static string PlatformId => Platform?.PlatformId ?? string.Empty;
    }
}
