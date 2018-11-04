using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Logging;

namespace Roadie.Library.MetaData
{
    public abstract class MetaDataProviderBase
    {
        protected readonly ICacheManager _cacheManager = null;
        protected readonly IRoadieSettings _configuration = null;
        protected readonly ILogger _loggingService = null;

        protected ApiKey _apiKey = null;

        public virtual bool IsEnabled
        {
            get
            {
                return true;
            }
        }

        protected ApiKey ApiKey
        {
            get
            {
                return this._apiKey;
            }
        }

        protected ICacheManager CacheManager
        {
            get
            {
                return this._cacheManager;
            }
        }

        protected IRoadieSettings Configuration
        {
            get
            {
                return this._configuration;
            }
        }

        protected ILogger Logger
        {
            get
            {
                return this._loggingService;
            }
        }

        public MetaDataProviderBase(IRoadieSettings configuration, ICacheManager cacheManager, ILogger loggingService)
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
    }
}