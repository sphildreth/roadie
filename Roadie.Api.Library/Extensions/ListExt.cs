using System;
using System.Collections.Generic;
using System.Linq;

namespace Roadie.Library.Extensions
{
    public static class ListExt
    {
        public static IEnumerable<T> Randomize<T>(this IEnumerable<T> source)
        {
            var rnd = new Random();
            return source.OrderBy(item => rnd.Next());
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            var rnd = new Random();
            while (n > 1)
            {
                var k = rnd.Next(0, n) % n;
                n--;
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static string ToDelimitedList<T>(this IList<T> list, char delimiter = '|')
        {
            return ((ICollection<T>)list).ToDelimitedList(delimiter);
        }

        public static string ToDelimitedList<T>(this IEnumerable<T> list, char delimiter = '|')
        {
            if (list == null || !list.Any()) return null;
            return string.Join(delimiter.ToString(), list);
        }
    }
}