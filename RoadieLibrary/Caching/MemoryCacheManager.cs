using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Caching
{
    public class MemoryCacheManager : CacheManagerBase
    {
        private MemoryCache _cache;

        public MemoryCacheManager(ILogger logger, CachePolicy defaultPolicy) 
            : base(logger, defaultPolicy)
        {

            this._cache = new MemoryCache(new MemoryCacheOptions());
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value)
        {
            using (var entry = _cache.CreateEntry(key))
            {
                _cache.Set(key, value);
                return true;
            }
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value, CachePolicy policy)
        {
            using (var entry = _cache.CreateEntry(key))
            {
                _cache.Set(key, value, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.Add(policy.ExpiresAfter) // new DateTimeOffset(DateTime.UtcNow, policy.ExpiresAfter)                  
                });
                return true;
            }
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value, string region, CachePolicy policy)
        {
            return this.Add(key, value, policy);
        }

        public override void Clear()
        {
            this._cache = new MemoryCache(new MemoryCacheOptions());
        }

        public override void ClearRegion(string region)
        {
            this.Clear();
        }

        public override void Dispose()
        {
          //  throw new NotImplementedException();
        }

        public override bool Exists<TOut>(string key)
        {
            return this.Get<TOut>(key) != null;
        }

        public override bool Exists<TOut>(string key, string region)
        {
            return this.Exists<TOut>(key);
        }

        public override TOut Get<TOut>(string key)
        {
            return this._cache.Get<TOut>(key);
        }

        public override TOut Get<TOut>(string key, string region)
        {
            return this.Get<TOut>(key);
        }

        public override TOut Get<TOut>(string key, Func<TOut> getItem, string region)
        {
            return this.Get<TOut>(key, getItem, region, this._defaultPolicy);
        }

        public override TOut Get<TOut>(string key, Func<TOut> getItem, string region, CachePolicy policy)
        {
            var r = this.Get<TOut>(key, region);
            if (r == null)
            {
                r = getItem();
                this.Add(key, r, region, policy);
            }
            return r;
        }

        public override bool Remove(string key)
        {
            this._cache.Remove(key);
            return true;
        }

        public override bool Remove(string key, string region)
        {
            return this.Remove(key);
        }
    }
}
