using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Encoding
{
    public class DummyHttpEncoder : IHttpEncoder
    {
        public string HtmlEncode(string s)
        {
            return s;
        }

        public string UrlDecode(string s)
        {
            return s;
        }

        public string UrlEncode(string s)
        {
            return s;
        }

        public string UrlEncodeBase64(byte[] input)
        {
            throw new NotImplementedException();
        }

        public string UrlEncodeBase64(string input)
        {
            throw new NotImplementedException();
        }
    }
}
