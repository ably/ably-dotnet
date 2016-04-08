using System;
using System.Reflection;
using IO.Ably.Transport;

namespace IO.Ably.Platform
{
    /// <summary>This class initializes dynamically-injected platform dependencies.</summary>
    public static class IoC
    {
        static readonly IPlatform Platform;

        /// <summary>Load AblyPlatform.dll, instantiate AblyPlatform.PlatformImpl type.</summary>
        static IoC()
        {
            AssemblyName aname = new AssemblyName( "AblyPlatform" );
            Assembly ass = Assembly.Load( aname );
            Type tpImpl = ass.GetType( "AblyPlatform.PlatformImpl" );
            if( null == tpImpl )
                throw new Exception( "Fatal error: AblyPlatform.dll doesn't contain AblyPlatform.PlatformImpl type" );
            object obj = Activator.CreateInstance( tpImpl );
            Platform = obj as IPlatform;
        }

        public static string GetConnectionString()
        {
            return Platform.GetConnectionString();
        }

        public static ICrypto Crypto => Platform.GetCrypto();

        public static ITransportFactory WebSockets => Platform.GetWebSocketsFactory();
    }
}