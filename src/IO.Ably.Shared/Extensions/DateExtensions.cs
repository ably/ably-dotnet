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
    }
}
