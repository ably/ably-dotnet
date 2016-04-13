using System;

namespace IO.Ably.AcceptanceTests
{
    public class DateHelper
    {
        public static DateTimeOffset CreateDate(int year, int month, int day, int hours = 0, int minutes = 0, int seconds = 0)
        {
            return new DateTimeOffset(year, month, day, hours, minutes, seconds, TimeSpan.Zero);
        }
    }
}