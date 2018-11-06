using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roadie.Models.ThirdPartyApi.Subsonic
{
    [Serializable]
    public class Credentials
    {
        public const string API_VERSION = "1.14.0";

        public string version { get; set; }
        public string appName { get; set; }
        public string salt { get; set; }
        public string user { get; set; }
        //public string token
        //{
        //    get { return _token; }
        //   // set { _token = BitConverter.ToString(Encoding.UTF8.GetBytes(value)).Replace("-", ""); }

        //}
        public string token { get; set; }

        public Credentials(string version, string appName, string user, string salt, string token)
        {
            this.version = version;
            this.appName = appName;
            this.user = user;
            this.salt = salt;
            this.token = token;
        }
    }
}
