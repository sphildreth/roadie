using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System.Threading.Tasks;

namespace Roadie.Library.Scrobble
{
    public abstract class ScrobblerIntegrationBase : IScrobblerIntegration
    {
        protected ICacheManager CacheManager { get; }

        protected IRoadieSettings Configuration { get; }

        protected IRoadieDbContext DbContext { get; }

        protected IHttpContext HttpContext { get; }

        protected ILogger Logger { get; }

        public abstract int SortOrder { get; }

        public ScrobblerIntegrationBase(
            IRoadieSettings configuration,
            ILogger logger,
            IRoadieDbContext dbContext,
            ICacheManager cacheManager,
            IHttpContext httpContext)
        {
            Logger = logger;
            Configuration = configuration;
            DbContext = dbContext;
            CacheManager = cacheManager;
            HttpContext = httpContext;
        }

        public abstract Task<OperationResult<bool>> NowPlaying(User roadieUser, ScrobbleInfo scrobble);

        public abstract Task<OperationResult<bool>> Scrobble(User roadieUser, ScrobbleInfo scrobble);
    }
}
