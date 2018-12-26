using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Utility
{
    public interface IHttpContext
    {
        string BaseUrl { get; set; }
        string ImageBaseUrl { get; set; }
    }
}
