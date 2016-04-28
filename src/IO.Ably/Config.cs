using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using IO.Ably.CustomSerialisers;
using IO.Ably.Encryption;
using IO.Ably.Platform;

namespace IO.Ably
{
    public static class Config
    {
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

        public static int ProtocolVersion = 1;

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