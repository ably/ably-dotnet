using System;

namespace IO.Ably
{
    public static class DateExtensions
    {
        public static readonly DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static DateTimeOffset FromUnixTimeInMilliseconds(this long unixTime)
        {
            return Epoch.AddMilliseconds(unixTime);
        }

        public static long ToUnixTimeInMilliseconds(this DateTimeOffset date)
        {
            return Convert.ToInt64((date - Epoch).TotalMilliseconds);
        }

        public static string TimeToString(this TimeSpan timeSpan)
        {
            //00:03:32.8289777
            return String.Format("{0}h:{1}m:{2}s.{3}ms", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
        }
    }
}
