using Microsoft.AspNetCore.Mvc;
using Roadie.Library.Configuration;
using Roadie.Library.Utility;

namespace Roadie.Api.Services
{
    public class HttpContext : IHttpContext
    {
        public string BaseUrl { get; set; }
        public string ImageBaseUrl { get; set; }

        public HttpContext(IRoadieSettings configuration, IUrlHelper urlHelper)
        {
            var scheme = urlHelper.ActionContext.HttpContext.Request.Scheme;
            if (configuration.UseSSLBehindProxy)
            {
                scheme = "https";
            }
            var host = urlHelper.ActionContext.HttpContext.Request.Host;
            if(!string.IsNullOrEmpty(configuration.BehindProxyHost))
            {
                host = new Microsoft.AspNetCore.Http.HostString(configuration.BehindProxyHost);
            }
            this.BaseUrl = $"{ scheme }://{ host }";
            this.ImageBaseUrl = $"{ this.BaseUrl}/images";
        }
    }
}