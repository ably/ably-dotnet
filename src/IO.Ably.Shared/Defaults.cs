using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using IO.Ably.Transport;

namespace IO.Ably
{
    internal static class Defaults
    {
        internal static readonly float ProtocolVersionNumber = 1.2F;

        internal static readonly string LibraryVersion = GetVersion();

        internal static string GetVersion()
        {
            var version = typeof(Defaults).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            return version.Split('.').Take(3).JoinStrings(".");
        }

        public static string ProtocolVersion { get; } = ProtocolVersionNumber.ToString(CultureInfo.InvariantCulture);

        public const int QueryLimit = 100;

        public const string InternetCheckUrl = "https://internet-up.ably-realtime.com/is-the-internet-up.txt";
        public const string InternetCheckOkMessage = "yes";

        public const string RestHost = "rest.ably.io";
        public const string RealtimeHost = "realtime.ably.io";

        public const int Port = 80;
        public const int TlsPort = 443;

        public static readonly string[] FallbackHosts;
        public static readonly TimeSpan DefaultTokenTtl = TimeSpan.FromHours(1);
        public static readonly Capability DefaultTokenCapability = Capability.AllowAll;

        // Buffer in seconds before a token is considered unusable
        public const int TokenExpireBufferInSeconds = 15;
        public const int HttpMaxRetryCount = 3;
        public static readonly TimeSpan ChannelRetryTimeout = TimeSpan.FromSeconds(15);
        public static readonly TimeSpan HttpMaxRetryDuration = TimeSpan.FromSeconds(15);
        public static readonly TimeSpan MaxHttpRequestTimeout = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan MaxHttpOpenTimeout = TimeSpan.FromSeconds(4);
        public static readonly TimeSpan DefaultRealtimeTimeout = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan DisconnectedRetryTimeout = TimeSpan.FromSeconds(15);
        public static readonly TimeSpan SuspendedRetryTimeout = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan ConnectionStateTtl = TimeSpan.FromSeconds(60);
        public static readonly TimeSpan FallbackRetryTimeout = TimeSpan.FromMinutes(10); // https://docs.ably.io/client-lib-development-guide/features/#TO3l10

        public static readonly ITransportFactory WebSocketTransportFactory = IoC.TransportFactory;

        internal const int TokenErrorCodesRangeStart = 40140;
        internal const int TokenErrorCodesRangeEnd = 40149;

        internal const string DeviceIdentityTokenHeader = "X-Ably-DeviceIdentityToken";
        internal const string DeviceSecretHeader = "X-Ably-DeviceSecret";

        /// <summary>The default log level you'll see in the debug output.</summary>
        internal const LogLevel DefaultLogLevel = LogLevel.Warning;

        internal static Func<DateTimeOffset> NowFunc()
        {
            return () => DateTimeOffset.UtcNow;
        }

#if MSGPACK
        internal const Protocol DefaultProtocol = IO.Ably.Protocol.MsgPack;
        internal const bool MsgPackEnabled = true;
#else
        internal const Protocol Protocol = IO.Ably.Protocol.Json;
        internal const bool MsgPackEnabled = false;

#endif

        static Defaults()
        {
            FallbackHosts = new[]
            {
                "a.ably-realtime.com",
                "b.ably-realtime.com",
                "c.ably-realtime.com",
                "d.ably-realtime.com",
                "e.ably-realtime.com",
            };
        }

        internal static string[] GetEnvironmentFallbackHosts(string environment)
        {
            return new[]
            {
                $"{environment}-a-fallback.ably-realtime.com",
                $"{environment}-b-fallback.ably-realtime.com",
                $"{environment}-c-fallback.ably-realtime.com",
                $"{environment}-d-fallback.ably-realtime.com",
                $"{environment}-e-fallback.ably-realtime.com",
            };
        }

        private static readonly string AblySdkIdentifier = $"ably-dotnet/{LibraryVersion}"; // RSC7d1

        internal static readonly string AgentHeaders = GenerateAgentHeaders();

        private static string GenerateAgentHeaders()
        {
            return $"{AblySdkIdentifier}";

            // string osPlatform = Environment.OSVersion.VersionString;
            // osPlatform = osPlatform.ToLower();
            // osPlatform = osPlatform.Replace(' ', '-');
            // //
            // // var sb = new StringBuilder();
            // // sb.Append("ably-dotnet/")
            // //     .Append(LibraryVersion)
            // //     .Append(" os-platform/")
            // //     .Append(osPlatform)
            // //     .Append(" runtime/")
            // //     .Append(Environment.Version);
            //
            // return sb.ToString();
        }
    }
}
