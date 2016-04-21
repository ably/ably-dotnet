using System;
using System.Collections.Generic;
using IO.Ably.Transport;

namespace IO.Ably
{
    internal static class Defaults
    {
        public const int ProtocolVersion = 1;
        public const int QueryLimit = 100;

        public const string InternetCheckURL = "https://internet-up.ably-realtime.com/is-the-internet-up.txt";
        public static readonly string InternetCheckOKMessage = "yes";

        public static readonly string RestHost = "rest.ably.io";
        public static readonly String RealtimeHost = "realtime.ably.io";
        public static readonly string[] FallbackHosts;
        public static readonly TimeSpan DefaultTokenTtl = TimeSpan.FromHours(1);
        public static readonly Capability DefaultTokenCapability = Capability.AllowAll;
        public const int Port = 80;
        public const int TlsPort = 443;
        // Buffer in seconds before a token is considered unusable
        public const int TokenExpireBufferInSeconds = 15;
        public static readonly string[] SupportedTransports = new string[]{ "web_socket" };
        public static readonly Dictionary<string, ITransportFactory> TransportFactories;

        internal const int TokenErrorCodesRangeStart = 40140;
        internal const int TokenErrorCodesRangeEnd = 40149;

        /// <summary>The default log level you'll see in the debug output.</summary>
        internal const LogLevel DefaultLogLevel = LogLevel.Warning;

        static Defaults()
        {
            Defaults.FallbackHosts = new string[] { "A.ably-realtime.com", "B.ably-realtime.com", "C.ably-realtime.com", "D.ably-realtime.com", "E.ably-realtime.com" };
            Defaults.TransportFactories = new Dictionary<string, ITransportFactory>()
            {
                { "web_socket", Platform.IoC.WebSockets }
            };
        }
    }
}