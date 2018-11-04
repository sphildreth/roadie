using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Encoding
{
    public interface IHttpEncoder
    {
        string UrlEncode(string s);
        string UrlDecode(string s);

        string HtmlEncode(string s);
    }
}
