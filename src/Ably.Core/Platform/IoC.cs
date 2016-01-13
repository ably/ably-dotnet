﻿using Ably.Transport;
using System;
using System.Reflection;

namespace Ably.Platform
{
    /// <summary>This class initializes dynamically-injected platform dependencies.</summary>
    public static class IoC
    {
        static IPlatform s_platform;

        /// <summary>Load AblyPlatform.dll, instantiate AblyPlatform.PlatformImpl type.</summary>
        static IoC()
        {
            AssemblyName aname = new AssemblyName( "AblyPlatform" );
            Assembly ass = Assembly.Load( aname );
            Type tpImpl = ass.GetType( "AblyPlatform.PlatformImpl" );
            if( null == tpImpl )
                throw new Exception( "Fatal error: AblyPlatform.dll doesn't contain AblyPlatform.PlatformImpl type" );
            object obj = Activator.CreateInstance( tpImpl );
            s_platform = obj as IPlatform;
        }

        public static string getConnectionString()
        {
            return s_platform.getConnectionString();
        }

        public static ICrypto crypto
        {
            get
            {
                return s_platform.getCrypto();
            }
        }

        public static ITransportFactory webSockets
        {
            get
            {
                return s_platform.getWebSocketsFactory();
            }
        }
    }
}