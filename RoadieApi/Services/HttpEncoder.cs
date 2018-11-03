using Roadie.Library.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Roadie.Api.Services
{
    public class HttpEncoder : IHttpEncoder
    {
        public string UrlDecode(string s)
        {
            return HttpUtility.UrlDecode(s);
        }

        public string UrlEncode(string s)
        {
            return HttpUtility.UrlEncode(s);
        }
    }
}
