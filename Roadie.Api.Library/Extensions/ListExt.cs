using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roadie.Library.Extensions
{
    public static class ListExt
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = StaticRandom.Instance.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
        {
            T[] elements = source.ToArray();
            for (int i = elements.Length - 1; i >= 0; i--)
            {
                int swapIndex = StaticRandom.Instance.Next(i + 1);
                yield return elements[swapIndex];
                elements[swapIndex] = elements[i];
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