using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Library.Scrobble
{
    public abstract class ScrobblerIntegrationBase : IScrobblerIntegration
    {
        protected ICacheManager CacheManager { get; }

        protected IRoadieSettings Configuration { get; }

        protected data.IRoadieDbContext DbContext { get; }

        protected IHttpContext HttpContext { get; }

        protected ILogger Logger { get; }

        public ScrobblerIntegrationBase(IRoadieSettings configuration, ILogger logger, data.IRoadieDbContext dbContext,
                                                    ICacheManager cacheManager, IHttpContext httpContext)
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