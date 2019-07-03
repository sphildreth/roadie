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
                var ex = new TException();
                // I could not figure out how to set message on a generic Error so I pushed it to Data with Predicate Result as Key
                ex.Data.Add(Predicate.ToString(), Message);
                throw ex;
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