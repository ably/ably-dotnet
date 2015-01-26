using System;

namespace Ably
{
    public static class DateExtensions
    {
        public static DateTimeOffset FromUnixTime(this long unixTime)
        {
            var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
            
            return epoch.AddSeconds(unixTime);
        }

        public static DateTimeOffset FromUnixTimeInMilliseconds(this long unixTime)
        {
            var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
            return epoch.AddMilliseconds(unixTime);
        }

        public static long ToUnixTimeInMilliseconds(this DateTimeOffset date)
        {
            var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
            return Convert.ToInt64((date - epoch).TotalMilliseconds);
        }

        public static long ToUnixTime(this DateTimeOffset date)
        {
            return ToUnixTime(date.DateTime);
        }

        public static DateTimeOffset ToDateTimeOffset(this DateTime dateTime)
        {
            return new DateTimeOffset(dateTime, TimeSpan.Zero);
        }

        private static long ToUnixTime(this DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date.ToUniversalTime() - epoch).TotalSeconds);
        }
    }
}