using System;

namespace Ably
{
    public static class Config
    {
        public static ILogger AblyLogger = Logger.Current;
        public static Func<CipherParams, IChannelCipher> GetCipher = @params => new AesCipher(@params);
        internal static string DefaultHost = "rest.ably.io";
        internal static Func<DateTime> Now = () => DateTime.Now;

    }
}