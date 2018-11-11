using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Roadie.Library.Caching
{
    public class DictionaryCacheManager : CacheManagerBase
    {
        private Dictionary<string, object> Cache { get; }

        public DictionaryCacheManager(ILogger logger, CachePolicy defaultPolicy)
            : base(logger, defaultPolicy)
        {
            this.Cache = new Dictionary<string, object>();
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value)
        {
            if(this.Cache.ContainsKey(key))
            {
                this.Cache.Remove(key);
            }
            this.Cache.Add(key, value);
            return true;
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value, string region)
        {
            return this.Add(key, value);
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value, CachePolicy policy)
        {
            return this.Add(key, value);
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value, string region, CachePolicy policy)
        {
            return this.Add(key, value);
        }

        public override void Clear()
        {
            this.Cache.Clear();
        }

        public override void ClearRegion(string region)
        {
            this.Clear();
        }

        public override bool Exists<TOut>(string key)
        {
            return this.Cache.ContainsKey(key);
        }

        public override bool Exists<TOut>(string key, string region)
        {
            return this.Exists<TOut>(key);
        }

        public override TOut Get<TOut>(string key)
        {
            if(!this.Cache.ContainsKey(key))
            {
                return default(TOut);
            }
            return (TOut)this.Cache[key];
        }

        public override TOut Get<TOut>(string key, string region)
        {
            return Get<TOut>(key);
        }

        public override TOut Get<TOut>(string key, Func<TOut> getItem, string region)
        {
            return Get<TOut>(key, getItem, region, this._defaultPolicy);
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
            if(this.Cache.ContainsKey(key))
            {
                this.Cache.Remove(key);
            }
            return true;
        }

        public override bool Remove(string key, string region)
        {
            return this.Remove(key);
        }

        public async override Task<TOut> GetAsync<TOut>(string key, Func<Task<TOut>> getItem, string region)
        {
            var r = this.Get<TOut>(key, region);
            if (r == null)
            {
                r = await getItem();
                this.Add(key, r, region);
                this.Logger.LogInformation($"-+> Cache Miss for Key [{ key }], Region [{ region }]");
            }
            else
            {
                this.Logger.LogInformation($"-!> Cache Hit for Key [{ key }], Region [{ region }]");
            }
            return r;
        }
    }
}