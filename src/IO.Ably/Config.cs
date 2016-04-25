using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using IO.Ably.CustomSerialisers;
using IO.Ably.Encryption;

namespace IO.Ably
{
    public static class Config
    {
        /// <summary>
        /// Http connection timeout in ms.
        /// Default value: 15000 ms
        /// </summary>
        public static Func<CipherParams, IChannelCipher> GetCipher = Crypto.GetCipher;

        /// <summary>X-Ably-Version HTTP request header value</summary>
        internal const string AblyVersion = "0.8";

        internal static Func<DateTimeOffset> Now = () => DateTimeOffset.UtcNow;
        
        public static string Host = "rest.ably.io";
        public const int Port = 80;
        public const int TlsPort = 443;
        public const int ConnectTimeout = 15000;
        public const int CommulativeFailedRequestTimeOutInSeconds = 10;
        public const int DisconnectTimeout = 10000;
        public const int SuspendedTimeout = 60000;
        public static string[] Transports = { "web_socket", "comet" };
        

        public static int ProtocolVersion = 1;

        static Config()
        {
            // Configure JSON serializer/de-serializer
            JsonConvert.DefaultSettings = GetJsonSettings;
        }

        internal static JsonSerializerSettings GetJsonSettings()
        {
            JsonSerializerSettings res = new JsonSerializerSettings();
            res.Converters = new List<JsonConverter>()
            {
                new DateTimeOffsetJsonConverter(),
                new CapabilityJsonConverter()
            };
            res.DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind;
            res.NullValueHandling = NullValueHandling.Ignore;
            return res;
        }
    }
}