using Mapster;
using Microsoft.AspNet.OData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;

namespace Roadie.Api.Controllers
{
    public abstract class EntityControllerBase : ODataController
    {
        protected readonly ICacheManager _cacheManager;
        protected readonly IConfiguration _configuration;
        protected readonly IRoadieSettings _roadieSettings;

        protected ILogger _logger;

        protected IRoadieSettings RoadieSettings
        {
            get
            {
                return this._roadieSettings;
            }
        }

        public EntityControllerBase(ICacheManager cacheManager, IConfiguration configuration)
        {
            this._cacheManager = cacheManager;
            this._configuration = configuration;

            this._roadieSettings = new RoadieSettings();
            this._configuration.GetSection("RoadieSettings").Bind(this._roadieSettings);

        }
    }
}