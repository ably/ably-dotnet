using System;

using IO.Ably;
using IO.Ably.Encryption;

namespace IO.Ably
{
    public static class Config
    {
        public static Func<CipherParams, IChannelCipher> GetCipher = Crypto.GetCipher;

        // internal static Func<DateTimeOffset> Now = () => DateTimeOffset.UtcNow;
        public static string Host = "rest.ably.io";
        public const int Port = 80;
        public const int TlsPort = 443;
        public const int ConnectTimeout = 15000;
        public const int CommulativeFailedRequestTimeOutInSeconds = 10;
        public const int DisconnectTimeout = 10000;
        public const int SuspendedTimeout = 60000;
        public static int ProtocolVersion = 1;

#if MSGPACK
        internal const bool MsgPackEnabled = true;
#else
        internal const bool MsgPackEnabled = false;
#endif
    }
}