using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.Caching
{
    /// <summary>
    ///     Redis Cache implemention
    /// </summary>
    /// <typeparam name="TCacheValue"></typeparam>
    public class RedisCacheManager : CacheManagerBase
    {
        private static readonly Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            return ConnectionMultiplexer.Connect("192.168.1.253:6379,allowAdmin=true");
        });

        private readonly bool _doTraceLogging = true;
        private IDatabase _redis;

        public static ConnectionMultiplexer Connection => lazyConnection.Value;

        private IDatabase Redis => _redis ?? (_redis = Connection.GetDatabase());

        public RedisCacheManager(ILogger logger, CachePolicy defaultPolicy)
                            : base(logger, defaultPolicy)
        {
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value)
        {
            return Add(key, value, null, _defaultPolicy);
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value, string region)
        {
            return Add(key, value, region, null);
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value, CachePolicy policy)
        {
            return Add(key, value, null, _defaultPolicy);
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value, string region, CachePolicy policy)
        {
            if (_doTraceLogging) Logger.LogTrace("Added [{0}], Region [{1}]", key, region);
            return Redis.StringSet(key, Serialize(value));
        }

        public override void Clear()
        {
            var endpoints = Connection.GetEndPoints();
            var server = Connection.GetServer(endpoints[0]);
            server.FlushAllDatabases();
            if (_doTraceLogging) Logger.LogTrace("Cleared Cache");
        }

        public override void ClearRegion(string region)
        {
            Clear();
        }

        public override bool Exists<TOut>(string key)
        {
            return Exists<TOut>(key, null);
        }

        public override bool Exists<TOut>(string key, string region)
        {
            return Redis.KeyExists(key);
        }

        public override TOut Get<TOut>(string key)
        {
            return Get<TOut>(key, null);
        }

        public override TOut Get<TOut>(string key, string region)
        {
            return Get<TOut>(key, region, _defaultPolicy);
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

        public override Task<TOut> GetAsync<TOut>(string key, Func<Task<TOut>> getItem, string region)
        {
            throw new NotImplementedException();
        }

        public override bool Remove(string key)
        {
            return Remove(key, null);
        }

        public override bool Remove(string key, string region)
        {
            if (Redis.KeyExists(key)) Redis.KeyDelete(key);
            return false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                try
                {
                    Connection.Close();
                    Connection.Dispose();
                }
                catch
                {
                }

            // free native resources here if there are any
        }

        private TOut Get<TOut>(string key, string region, CachePolicy policy)
        {
            var result = Deserialize<TOut>(Redis.StringGet(key));
            if (result == null)
            {
                if (_doTraceLogging) Logger.LogTrace("Get Cache Miss Key [{0}], Region [{1}]", key, region);
            }
            else if (_doTraceLogging)
            {
                Logger.LogTrace("Get Cache Hit Key [{0}], Region [{1}]", key, region);
            }

            return result;
        }
    }

    internal class IgnoreJsonAttributesResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var props = base.CreateProperties(type, memberSerialization);
            foreach (var prop in props)
            {
                prop.Ignored = false; // Ignore [JsonIgnore]
                prop.Converter = null; // Ignore [JsonConverter]
                prop.PropertyName = prop.UnderlyingName; // restore original property name
            }

            return props;
        }
    }
}