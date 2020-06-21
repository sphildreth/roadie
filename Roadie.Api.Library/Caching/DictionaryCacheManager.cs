using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.Caching
{
    public class DictionaryCacheManager : CacheManagerBase
    {
        private static readonly object padlock = new object();

        private Dictionary<string, object> Cache { get; }

        public DictionaryCacheManager(ILogger logger, ICacheSerializer cacheSerializer, CachePolicy defaultPolicy)
                    : base(logger, cacheSerializer, defaultPolicy)
        {
            Cache = new Dictionary<string, object>();
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value)
        {
            lock (padlock)
            {
                if (Cache.ContainsKey(key)) Cache.Remove(key);
                Cache.Add(key, value);
                return true;
            }
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value, string region)
        {
            return Add(key, value);
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value, CachePolicy policy)
        {
            return Add(key, value);
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value, string region, CachePolicy policy)
        {
            return Add(key, value);
        }

        public override void Clear()
        {
            lock (padlock)
            {
                Cache.Clear();
            }
        }

        public override void ClearRegion(string region)
        {
            lock (padlock)
            {
                Clear();
            }
        }

        public override bool Exists<TOut>(string key)
        {
            lock (padlock)
            {
                return Cache.ContainsKey(key);
            }
        }

        public override bool Exists<TOut>(string key, string region)
        {
            lock (padlock)
            {
                return Exists<TOut>(key);
            }
        }

        public override TOut Get<TOut>(string key)
        {
            lock (padlock)
            {
                if (!Cache.ContainsKey(key)) return default(TOut);
                return (TOut)Cache[key];
            }
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
            lock (padlock)
            {
                var r = Get<TOut>(key, region);
                if (r == null)
                {
                    r = getItem();
                    Add(key, r, region, policy);
                }

                return r;
            }
        }

        public override async Task<TOut> GetAsync<TOut>(string key, Func<Task<TOut>> getItem, string region)
        {
            var r = Get<TOut>(key, region);
            if (r == null)
            {
                r = await getItem().ConfigureAwait(false);
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
            lock (padlock)
            {
                if (Cache.ContainsKey(key)) Cache.Remove(key);
                return true;
            }
        }

        public override bool Remove(string key, string region)
        {
            return Remove(key);
        }
    }
}