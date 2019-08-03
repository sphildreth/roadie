using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Roadie.Library.Caching
{
    public class MemoryCacheManager : CacheManagerBase
    {
        private MemoryCache _cache;

        public MemoryCacheManager(ILogger logger, CachePolicy defaultPolicy)
            : base(logger, defaultPolicy)
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value)
        {
            _cache.Set(key, value);
            return true;
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value, string region)
        {
            _cache.Set(key, value, _defaultPolicy.ExpiresAfter);
            return true;
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value, CachePolicy policy)
        {
            _cache.Set(key, value, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration =
                    DateTimeOffset.UtcNow.Add(policy
                        .ExpiresAfter) // new DateTimeOffset(DateTime.UtcNow, policy.ExpiresAfter)
            });
            return true;
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value, string region, CachePolicy policy)
        {
            return Add(key, value, policy);
        }

        public override void Clear()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        public override void ClearRegion(string region)
        {
            Clear();
        }

        public override bool Exists<TOut>(string key)
        {
            return Get<TOut>(key) != null;
        }

        public override bool Exists<TOut>(string key, string region)
        {
            return Exists<TOut>(key);
        }

        public override TOut Get<TOut>(string key)
        {
            return _cache.Get<TOut>(key);
        }

        public override TOut Get<TOut>(string key, string region)
        {
            return Get<TOut>(key);
        }

        public override TOut Get<TOut>(string key, Func<TOut> getItem, string region)
        {
            return Get(key, getItem, region, _defaultPolicy);
        }

        public override TOut Get<TOut>(string key, Func<TOut> getItem, string region, CachePolicy policy)
        {
            var r = Get<TOut>(key, region);
            if (r == null)
            {
                r = getItem();
                Add(key, r, region, policy);
            }

            return r;
        }

        public override async Task<TOut> GetAsync<TOut>(string key, Func<Task<TOut>> getItem, string region)
        {
            var r = Get<TOut>(key, region);
            if (r == null)
            {
                r = await getItem();
                Add(key, r, region);
                Logger.LogTrace($"-+> Cache Miss for Key [{key}], Region [{region}]");
            }
            else
            {
                Logger.LogTrace($"-!> Cache Hit for Key [{key}], Region [{region}]");
            }

            return r;
        }

        public override bool Remove(string key)
        {
            _cache.Remove(key);
            return true;
        }

        public override bool Remove(string key, string region)
        {
            return Remove(key);
        }
    }
}