using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;

namespace Roadie.Library.Caching
{
    public class NewtonsoftCacheSerializer : ICacheSerializer
    {
        protected ILogger Logger { get; }

        protected readonly JsonSerializerSettings _serializerSettings;

        public NewtonsoftCacheSerializer(ILogger logger)
        {
            Logger = logger;
            _serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new IgnoreJsonAttributesResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
            };
        }

        public TOut Deserialize<TOut>(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return default(TOut);
            }
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

        public string Serialize(object o) => JsonConvert.SerializeObject(o, _serializerSettings);
    }
}