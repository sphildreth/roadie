using System;

namespace Roadie.Library.Configuration
{
    [Serializable]
    public class RedisCache : IRedisCache
    {
        public string ConnectionString { get; set; }
    }
}