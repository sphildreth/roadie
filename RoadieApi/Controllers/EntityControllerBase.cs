using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Library.Configuration;
using Roadie.Library.Caching;
using Roadie.Library.Data;

namespace Roadie.Api.Controllers
{
    public abstract class EntityControllerBase : ODataController
    {
        protected readonly ICacheManager _cacheManager;
        protected readonly IConfiguration _configuration;
        protected readonly IRoadieDbContext _RoadieDbContext;

        protected ILogger _logger;

        public EntityControllerBase(IRoadieDbContext RoadieDbContext, ICacheManager cacheManager, IConfiguration configuration)
        {
            this._RoadieDbContext = RoadieDbContext;
            this._cacheManager = cacheManager;
            this._configuration = configuration;
        }
    }
}