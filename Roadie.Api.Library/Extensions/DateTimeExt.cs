using System;

namespace Roadie.Library.Extensions
{
    public static class DateTimeExt
    {
        public static DateTime? FormatDateTime(this DateTime? value)
        {
            if (!value.HasValue) return null;
            if (value.Value.Year == 0 || value.Value.Year == 1) return null;
            return value;
        }

        public static DateTime FromUnixTime(this long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        public static long ToUnixTime(this DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date - epoch).TotalSeconds);
        }

        public static int ToUnixTimeSinceEpoch(this DateTime dateTime)
        {
            var utcDateTime = dateTime.ToUniversalTime();
            var jan1St1970 = new DateTime(1970, 1, 1, 0, 0, 0);

            var timeSinceJan1St1970 = utcDateTime.Subtract(jan1St1970);
            return (int)timeSinceJan1St1970.TotalSeconds;
        }
    }
}