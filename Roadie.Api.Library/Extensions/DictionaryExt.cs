using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roadie.Library.Extensions
{
    public static class EnumerableExt
    {
        public static string ToTimings(this IDictionary<string, long> values)
        {
            if(values == null || !values.Any())
            {
                return null;
            }
            var timings = new List<string>
            {
                $" TOTAL: { values.Sum(x => x.Value) }"
            };
            foreach (var timing in values.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
            {
                timings.Add($"{ timing.Key}: { timing.Value }");
            }
            return string.Join(", ", timings);
        }


    }
}
