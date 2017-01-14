using System;
using System.Reflection;
using IO.Ably.Transport;

namespace IO.Ably
{
    internal static class Defaults
    {
        public const string ProtocolVersion = "0.8";
        private static readonly AssemblyName AssemblyName = typeof(Defaults).Assembly.GetName();

        public static string LibraryVersion => $"dotnet-{AssemblyName.Version.ToString(3)}";

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

        public static readonly ITransportFactory WebSocketTransportFactory = Platform.IoC.WebSockets;

        internal const int TokenErrorCodesRangeStart = 40140;
        internal const int TokenErrorCodesRangeEnd = 40149;

        /// <summary>The default log level you'll see in the debug output.</summary>
        internal const LogLevel DefaultLogLevel = LogLevel.Warning;

        static Defaults()
        {
            FallbackHosts = new[] { "a.ably-realtime.com", "b.ably-realtime.com", "c.ably-realtime.com", "d.ably-realtime.com", "e.ably-realtime.com" };
        }
    }
}