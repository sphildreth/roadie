using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roadie.Library.Extensions
{
    public static class IntEx
    {
        public static int? Or(this int? value, int? alternative)
        {
            if (!value.HasValue && !alternative.HasValue)
            {
                return null;
            }
            return value.HasValue ? value : alternative;
        }
    }
}
