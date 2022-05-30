using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using System;
using System.Net;
using System.Net.Http;

namespace Roadie.Library.MetaData
{
    public abstract class MetaDataProviderBase
    {
        private Lazy<HttpClient> _httpClient;

        private readonly IHttpClientFactory _httpClientFactory;

        protected readonly ICacheManager _cacheManager;

        protected readonly IRoadieSettings _configuration;

        protected readonly ILogger _logger;

        protected IApiKey _apiKey = null;

        public virtual bool IsEnabled => true;

        protected IApiKey ApiKey => _apiKey;

        protected ICacheManager CacheManager => _cacheManager;

        protected IRoadieSettings Configuration => _configuration;

        protected ILogger Logger => _logger;

        protected HttpClient HttpClient => _httpClient.Value;

        public MetaDataProviderBase(
            IRoadieSettings configuration,
            ICacheManager cacheManager,
            ILogger logger,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _cacheManager = cacheManager;
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            _httpClient = new Lazy<HttpClient>(() => _httpClientFactory.CreateClient());

            ServicePointManager.ServerCertificateValidationCallback += delegate
            {
                return true; // **** Always accept
            };
        }
    }
}
