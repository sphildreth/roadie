using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roadie.Library.Extensions
{
    public static class LongExt
    {
        public static string ToFileSize(this long? l)
        {
            if(!l.HasValue)
            {
                return "0";
            }
            return String.Format(new FileSizeFormatProvider(), "{0:fs}", l);
        }
    }
}
