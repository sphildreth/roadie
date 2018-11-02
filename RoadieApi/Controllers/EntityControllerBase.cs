using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using roadie.Library.Setttings;
using Roadie.Library.Caching;
using Roadie.Library.Data;

namespace Roadie.Api.Controllers
{
    public abstract class EntityControllerBase : ODataController
    {
        protected readonly ICacheManager _cacheManager;
        protected readonly IConfiguration _configuration;
        protected readonly IRoadieDbContext _roadieDbContext;
        protected readonly IRoadieSettings _roadieSettings;

        protected ILogger _logger;

        public EntityControllerBase(IRoadieDbContext roadieDbContext, ICacheManager cacheManager, IConfiguration configuration, IRoadieSettings roadieSettings)
        {
            this._roadieDbContext = roadieDbContext;
            this._cacheManager = cacheManager;
            this._configuration = configuration;
            this._roadieSettings = roadieSettings;
        }
    }
}