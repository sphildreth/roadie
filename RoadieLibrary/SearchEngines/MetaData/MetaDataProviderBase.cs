using Microsoft.Extensions.Configuration;
using Roadie.Library.Caching;
using Roadie.Library.Logging;
using Roadie.Library.Setttings;
using System.Net;

namespace Roadie.Library.MetaData
{
    public abstract class MetaDataProviderBase
    {
        protected readonly IConfiguration _configuration = null;
        protected readonly ICacheManager _cacheManager = null;
        protected readonly ILogger _loggingService = null;

        protected ICacheManager CacheManager
        {
            get
            {
                return this._cacheManager;
            }
        }

        protected ILogger Logger
        {
            get
            {
                return this._loggingService;
            }
        }

        protected IConfiguration Configuration
        {
            get
            {
                return this._configuration;
            }
        }

        protected ApiKey _apiKey = null;
        protected ApiKey ApiKey
        {
            get
            {
                return this._apiKey;
            }
        }

        public MetaDataProviderBase(IConfiguration configuration, ICacheManager cacheManager, ILogger loggingService)
        {
            this._configuration = configuration;
            this._cacheManager = cacheManager;
            this._loggingService = loggingService;

            System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                        System.Security.Cryptography.X509Certificates.X509Chain chain,
                        System.Net.Security.SslPolicyErrors sslPolicyErrors)
            {
                return true; // **** Always accept
            };

        }

        public virtual bool IsEnabled
        {
            get
            {
                return true;
            }
        }
    }
}