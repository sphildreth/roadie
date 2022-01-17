using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Encoding;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public abstract class HttpFactoryServiceBase<T> : ServiceBase
    {
        public IHttpClientFactory HttpClientFactory { get; }

        public HttpFactoryServiceBase(
            IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IRoadieDbContext dbContext,
            ICacheManager cacheManager,
            ILogger<T> logger,
            IHttpContext httpContext,
            IHttpClientFactory httpClientFactory
        )
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
            HttpClientFactory = httpClientFactory;
        }

    }
}
