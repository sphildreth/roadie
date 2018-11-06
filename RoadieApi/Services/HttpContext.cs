using Microsoft.AspNetCore.Mvc;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public class HttpContext : IHttpContext
    {
        public string BaseUrl { get; set; }
        public string ImageBaseUrl { get; set; }


        public HttpContext(IUrlHelper urlHelper)
        {
            this.BaseUrl = $"{ urlHelper.ActionContext.HttpContext.Request.Scheme}://{ urlHelper.ActionContext.HttpContext.Request.Host }";
            this.ImageBaseUrl = $"{ this.BaseUrl}/image";
        }        
    }
}
