using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roadie.Library.Setttings
{
    /// <summary>
    /// This is a Api Key used by Roadie to interact with an API (ie KeyName is "BingImageSearch" and its key is the BingImageSearch Key)
    /// </summary>
    [Serializable]
    public class ApiKey
    {
        public string ApiName { get; set; }
        public string Key { get; set; }
        public string Secret { get; set; }

    }
}
