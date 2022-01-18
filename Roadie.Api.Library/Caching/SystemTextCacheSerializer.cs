using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Caching
{
    public sealed class SystemTextCacheSerializer : ICacheSerializer
    {
        private ILogger Logger { get; }

        public SystemTextCacheSerializer(ILogger logger)
        {
            Logger = logger;
        }

        public string Serialize(object o)
        {
            if (o == null)
            {
                return null;
            }
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(o);
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
                return System.Text.Json.JsonSerializer.Deserialize<TOut>(s);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            return default(TOut);
        }
    }
}
