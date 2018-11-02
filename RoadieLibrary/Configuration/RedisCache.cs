using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace roadie.Library.Setttings
{
    [Serializable]
    public class RedisCache
    {
        public string ConnectionString { get; set; }
    }
}
