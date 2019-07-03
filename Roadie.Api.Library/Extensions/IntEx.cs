using System;

namespace Roadie.Library.Extensions
{
    public static class IntEx
    {
        public static int? Or(this int? value, int? alternative)
        {
            if (!value.HasValue && !alternative.HasValue) return null;
            return value.HasValue ? value : alternative;
        }

        public static int ToSecondsFromMilliseconds(this int? value)
        {
            if (value > 0)
            {
                var contentDurationTimeSpan = TimeSpan.FromMilliseconds(value ?? 0);
                return (int)contentDurationTimeSpan.TotalSeconds;
            }

            return 0;
        }
    }
}