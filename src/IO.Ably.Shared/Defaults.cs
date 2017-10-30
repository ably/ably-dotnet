using System;
using System.Linq;
using System.Reflection;
using IO.Ably;
using IO.Ably.Transport;

namespace IO.Ably
{
    internal static class Defaults
    {
        public const string ProtocolVersion = "0.8";
        private static readonly string AssemblyVersion = GetVersion();

        private static string GetVersion()
        {
            var version =  typeof(Defaults).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            return version.Split('.').Take(3).JoinStrings(".");
        }

        public static string LibraryVersion => $"dotnet-{AssemblyVersion}";

        public const int QueryLimit = 100;

        public const string InternetCheckUrl = "https://internet-up.ably-realtime.com/is-the-internet-up.txt";
        public static readonly string InternetCheckOkMessage = "yes";

        public static readonly string RestHost = "rest.ably.io";
        public static readonly string RealtimeHost = "realtime.ably.io";
        public static readonly string[] FallbackHosts;
        public static readonly TimeSpan DefaultTokenTtl = TimeSpan.FromHours(1);
        public static readonly Capability DefaultTokenCapability = Capability.AllowAll;
        public const int Port = 80;
        public const int TlsPort = 443;
        // Buffer in seconds before a token is considered unusable
        public const int TokenExpireBufferInSeconds = 15;
        public static readonly TimeSpan DefaultRealtimeTimeout = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan DisconnectedRetryTimeout = TimeSpan.FromSeconds(15);
        public static readonly TimeSpan ConnectionStateTtl = TimeSpan.FromSeconds(60);

        public static readonly ITransportFactory WebSocketTransportFactory = IoC.TransportFactory;

        internal const int TokenErrorCodesRangeStart = 40140;
        internal const int TokenErrorCodesRangeEnd = 40149;

        /// <summary>The default log level you'll see in the debug output.</summary>
        internal const LogLevel DefaultLogLevel = LogLevel.Warning;

        internal static Func<DateTimeOffset> NowFunc()
        {
            return () => DateTimeOffset.UtcNow;
        }

#if MSGPACK
        internal const Protocol DefaultProtocol = IO.Ably.Protocol.MsgPack;
#else
        internal const Protocol Protocol = IO.Ably.Protocol.Json;
#endif

        static Defaults()
        {
            FallbackHosts = new[] { "a.ably-realtime.com", "b.ably-realtime.com", "c.ably-realtime.com", "d.ably-realtime.com", "e.ably-realtime.com" };
        }
        

    }
}