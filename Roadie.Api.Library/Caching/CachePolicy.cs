using System;

namespace Roadie.Library.Caching
{
    public sealed class CachePolicy
    {
        public TimeSpan ExpiresAfter { get; }

        /// <summary>
        ///     If specified, each read of the item from the cache will reset the expiration time
        /// </summary>
        public bool RenewLeaseOnAccess { get; }

        public CachePolicy(TimeSpan expiresAfter, bool renewLeaseOnAccess = false)
        {
            ExpiresAfter = expiresAfter;
            RenewLeaseOnAccess = renewLeaseOnAccess;
        }
    }
}