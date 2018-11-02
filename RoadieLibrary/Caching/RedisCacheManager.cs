using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Caching
{
    /// <summary>
    /// Redis Cache implemention
    /// </summary>
    /// <typeparam name="TCacheValue"></typeparam>
    public class RedisCacheManager : CacheManagerBase
    {
        private bool _doTraceLogging = true;

        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            return ConnectionMultiplexer.Connect("192.168.1.253:6379,allowAdmin=true");
        });

        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }

        private IDatabase _redis = null;
        private IDatabase Redis
        {
            get
            {
                return this._redis ?? (this._redis = Connection.GetDatabase());
            }
        }

        public RedisCacheManager(ILogger logger, CachePolicy defaultPolicy) 
            : base(logger, defaultPolicy)
        {
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value)
        {
            return this.Add(key, value, this._defaultPolicy);
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value, CachePolicy policy)
        {
            return this.Add(key, value, null, this._defaultPolicy);
        }

        public override bool Add<TCacheValue>(string key, TCacheValue value, string region, CachePolicy policy)
        {
            if (this._doTraceLogging)
            {
                this._logger.LogTrace("Added [{0}], Region [{1}]", key, region);
            }
            return this.Redis.StringSet(key, this.Serialize(value));
        }

        public override void Clear()
        {
            var endpoints = Connection.GetEndPoints();
            var server = Connection.GetServer(endpoints[0]);
            server.FlushAllDatabases();
            if (this._doTraceLogging)
            {
                this._logger.LogTrace("Cleared Cache");
            }
        }

        public override void ClearRegion(string region)
        {
            this.Clear();
        }

        // Dispose() calls Dispose(true)
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    Connection.Close();
                    Connection.Dispose();
                }
                catch
                {
                }
            }
            // free native resources here if there are any
        }

        public override bool Exists<TOut>(string key)
        {
            return this.Exists<TOut>(key, null);
        }

        public override bool Exists<TOut>(string key, string region)
        {
            return this.Redis.KeyExists(key);
        }

        public override TOut Get<TOut>(string key)
        {
            return this.Get<TOut>(key, null);
        }

        public override TOut Get<TOut>(string key, string region)
        {
            return this.Get<TOut>(key, region, this._defaultPolicy);
        }

        public override TOut Get<TOut>(string key, Func<TOut> getItem, string region)
        {
            return this.Get<TOut>(key, getItem, region, this._defaultPolicy);
        }

        private TOut Get<TOut>(string key, string region, CachePolicy policy)
        {
            var result = this.Deserialize<TOut>(this.Redis.StringGet(key));
            if (result == null)
            {
                if (this._doTraceLogging)
                {
                    this._logger.LogTrace("Get Cache Miss Key [{0}], Region [{1}]", key, region);
                }
            }
            else if (this._doTraceLogging)
            {
                this._logger.LogTrace("Get Cache Hit Key [{0}], Region [{1}]", key, region);
            }
            return result;
        }


        public override TOut Get<TOut>(string key, Func<TOut> getItem, string region, CachePolicy policy)
        {
            var r = this.Get<TOut>(key, region);
            if(r == null)
            {
                r = getItem();
                this.Add(key, r, region, policy);
            }
            return r;
        }


        public override bool Remove(string key)
        {
            return this.Remove(key, null);
        }


        public override bool Remove(string key, string region)
        {
            if (this.Redis.KeyExists(key))
            {
                this.Redis.KeyDelete(key);
            }
            return false;
        }
    }

    class IgnoreJsonAttributesResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
            foreach (var prop in props)
            {
                prop.Ignored = false;   // Ignore [JsonIgnore]
                prop.Converter = null;  // Ignore [JsonConverter]
                prop.PropertyName = prop.UnderlyingName;  // restore original property name
            }
            return props;
        }
    }
}
