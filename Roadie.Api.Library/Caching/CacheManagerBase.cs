using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Roadie.Library.Caching
{
    public abstract class CacheManagerBase : ICacheManager
    {
        public const string SystemCacheRegionUrn = "urn:system";

        protected readonly CachePolicy _defaultPolicy;
        protected readonly JsonSerializerSettings _serializerSettings;

        protected ILogger Logger { get; }

        public CacheManagerBase(ILogger logger, CachePolicy defaultPolicy)
        {
            Logger = logger;
            _defaultPolicy = defaultPolicy;
            _serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new IgnoreJsonAttributesResolver(),
                Formatting = Formatting.Indented
            };
        }

        public abstract bool Add<TCacheValue>(string key, TCacheValue value);

        public abstract bool Add<TCacheValue>(string key, TCacheValue value, CachePolicy policy);

        public abstract bool Add<TCacheValue>(string key, TCacheValue value, string region);

        public abstract bool Add<TCacheValue>(string key, TCacheValue value, string region, CachePolicy policy);

        public abstract void Clear();

        public abstract void ClearRegion(string region);

        public abstract bool Exists<TOut>(string key);

        public abstract bool Exists<TOut>(string key, string region);

        public abstract TOut Get<TOut>(string key);

        public abstract TOut Get<TOut>(string key, string region);

        public abstract TOut Get<TOut>(string key, Func<TOut> getItem, string region);

        public abstract TOut Get<TOut>(string key, Func<TOut> getItem, string region, CachePolicy policy);

        public abstract Task<TOut> GetAsync<TOut>(string key, Func<Task<TOut>> getItem, string region);

        public abstract bool Remove(string key);

        public abstract bool Remove(string key, string region);

        protected TOut Deserialize<TOut>(string s)
        {
            if (string.IsNullOrEmpty(s)) return default(TOut);
            try
            {
                return JsonConvert.DeserializeObject<TOut>(s, _serializerSettings);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            return default(TOut);
        }

        protected string Serialize(object o)
        {
            return JsonConvert.SerializeObject(o, _serializerSettings);
        }
    }
}