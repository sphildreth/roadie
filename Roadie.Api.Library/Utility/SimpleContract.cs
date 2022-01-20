using System;

namespace Roadie.Library.Utility
{
    public static class SimpleContract
    {
        /// <summary>
        ///     Test that Predicate is True if not then throw Exception
        /// </summary>
        /// <typeparam name="TException">Exception Type To Throw</typeparam>
        /// <param name="Predicate">Predicate to Test (Must Test True)</param>
        /// <param name="Message">Message For Exception</param>
        public static void Requires<TException>(bool Predicate, string Message)
            where TException : Exception, new()
        {
            if (!Predicate)
            {
                throw (TException)Activator.CreateInstance(typeof(TException), Message);
            }
        }

        /// <summary>
        ///     Test that Predicate is True if not then throw Exception
        /// </summary>
        /// <param name="Predicate">Predicate to Test (Must Test True)</param>
        /// <param name="Message">Message For Exception</param>
        public static void Requires(bool Predicate, string Message)
        {
            if (!Predicate) throw new Exception(Message);
        }
    }
}