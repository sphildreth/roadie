using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roadie.Library.Extensions
{
    public static class DateTimeExt
    {
        public static DateTime? FormatDateTime(this DateTime? value)
        {
            if(!value.HasValue)
            {
                return null;
            }
            if(value.Value.Year == 0 || value.Value.Year == 1)
            {
                return null;
            }
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
    }
}
