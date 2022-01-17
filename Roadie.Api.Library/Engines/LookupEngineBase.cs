using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Encoding;
using System.Net.Http;

namespace Roadie.Library.Engines
{
    public abstract class LookupEngineBase
    {
        protected ICacheManager CacheManager { get; }

        protected IRoadieSettings Configuration { get; }

        protected IRoadieDbContext DbContext { get; }

        protected IHttpEncoder HttpEncoder { get; }

        protected ILogger Logger { get; }

        protected IHttpClientFactory HttpClientFactory { get; }

        public LookupEngineBase(
            IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IRoadieDbContext context,
            ICacheManager cacheManager,
            ILogger logger,
            IHttpClientFactory httpclientFactory)
        {
            Configuration = configuration;
            HttpEncoder = httpEncoder;
            DbContext = context;
            CacheManager = cacheManager;
            Logger = logger;
            HttpClientFactory = httpclientFactory;
        }
    }
}