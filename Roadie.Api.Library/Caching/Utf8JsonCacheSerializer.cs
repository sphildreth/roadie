using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Utf8Json;

namespace Roadie.Library.Caching
{
    public class Utf8JsonCacheSerializer : ICacheSerializer
    {
        protected ILogger Logger { get; }

        public Utf8JsonCacheSerializer(ILogger logger)
        {
            Logger = logger;
        }

        public string Serialize(object o)
        {
            if(o == null)
            {
                return null;
            }
            try
            {
                return System.Text.Encoding.UTF8.GetString(JsonSerializer.Serialize(o));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            return null;
        }

        public TOut Deserialize<TOut>(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return default(TOut);
            }
            try
            {
                return JsonSerializer.Deserialize<TOut>(s);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            return default(TOut);
        }
    }
}
