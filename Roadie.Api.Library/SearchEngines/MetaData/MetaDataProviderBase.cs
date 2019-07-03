using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using System.Net;

namespace Roadie.Library.MetaData
{
    public abstract class MetaDataProviderBase
    {
        protected readonly ICacheManager _cacheManager;
        protected readonly IRoadieSettings _configuration;
        protected readonly ILogger _logger;

        protected IApiKey _apiKey = null;

        public virtual bool IsEnabled => true;

        protected IApiKey ApiKey => _apiKey;

        protected ICacheManager CacheManager => _cacheManager;

        protected IRoadieSettings Configuration => _configuration;

        protected ILogger Logger => _logger;

        public MetaDataProviderBase(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger)
        {
            _configuration = configuration;
            _cacheManager = cacheManager;
            _logger = logger;

            ServicePointManager.ServerCertificateValidationCallback += delegate
            {
                return true; // **** Always accept
            };
        }
    }
}