using System;

namespace Roadie.Library.Extensions
{
    public static class DecimalExt
    {
        public static int ToSecondsFromMilliseconds(this decimal? value)
        {
            if (value > 0)
            {
                var contentDurationTimeSpan = TimeSpan.FromMilliseconds((double)(value ?? 0));
                return (int)contentDurationTimeSpan.TotalSeconds;
            }
            return 0;
        }

        public static TimeSpan? ToTimeSpan(this decimal? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return TimeSpan.FromSeconds((double)value);
        }
    }
}