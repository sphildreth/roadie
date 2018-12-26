using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Extensions
{
    public static class TimeSpanExt
    {
        public static string ToDuration(this TimeSpan input)
        {
            if(input == null || input.TotalMilliseconds == 0)
            {
                return "--/--/--";
            }
            return input.ToString(@"ddd\.hh\:mm\:ss");
        }
    }
}
