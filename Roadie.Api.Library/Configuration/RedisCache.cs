using System;

namespace Roadie.Library.Configuration
{
    [Serializable]
    public class RedisCache
    {
        public string ConnectionString { get; set; }
    }
}