using Ably.Types;
using System;
using System.Collections.Generic;

namespace Ably.Transport
{
    internal static class Defaults
    {
        public const int ProtocolVersion = 1;
        public static readonly string RestHost = "rest.ably.io";
        public static readonly String RealtimeHost = "realtime.ably.io";
        public static readonly string[] FallbackHosts;
        public const int Port = 80;
        public const int TlsPort = 443;
        public const int ConnectTimeout = 15000;
        public const int DisconnectTimeout = 30000;
        public const int SuspendedTimeout = 120000;
        public const int CometRecvTimeout = 90000;
        public const int CometSendTimeout = 10000;
        public static readonly string[] SupportedTransports = new string[]{ "web_socket" };
        public static readonly Dictionary<string, ITransportFactory> TransportFactories;

        static Defaults()
        {
            Defaults.FallbackHosts = new string[] { "A.ably-realtime.com", "B.ably-realtime.com", "C.ably-realtime.com", "D.ably-realtime.com", "E.ably-realtime.com" };
            Defaults.TransportFactories = new Dictionary<string, ITransportFactory>()
            {
                { "web_socket", new WebSocketTransport.WebSocketTransportFactory() }
            };
        }
    }
}
