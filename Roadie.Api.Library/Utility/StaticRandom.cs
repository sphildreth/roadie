using System;
using System.Threading;

namespace Roadie.Library.Utility
{
    public static class StaticRandom
    {
        private static readonly ThreadLocal<Random> threadLocal = new ThreadLocal<Random> (() => new Random(Interlocked.Increment(ref seed)));

        private static int seed;
        public static Random Instance => threadLocal.Value;

        static StaticRandom()
        {
            seed = Environment.TickCount;
        }
    }
}