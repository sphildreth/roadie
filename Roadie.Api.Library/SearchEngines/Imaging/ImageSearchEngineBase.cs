using Microsoft.Extensions.Logging;
using RestSharp;
using Roadie.Library.Configuration;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.Imaging
{
    public abstract class ImageSearchEngineBase : IImageSearchEngine
    {
        protected readonly RestClient _client;

        protected readonly IRoadieSettings _configuratio;

        protected readonly string _referrer;

        protected readonly string _requestIp;

        protected IApiKey _apiKey = null;

        protected ILogger _logger;

        public abstract bool IsEnabled { get; }

        protected IApiKey ApiKey => _apiKey;

        protected IRoadieSettings Configuration => _configuratio;

        protected ILogger Logger => _logger;

        public ImageSearchEngineBase(IRoadieSettings configuration, ILogger logger, string baseUrl,
                                    string requestIp = null, string referrer = null)
        {
            _configuratio = configuration;
            if (string.IsNullOrEmpty(referrer) || referrer.StartsWith("http://localhost"))
                referrer = "http://github.com/sphildreth/Roadie";
            _referrer = referrer;
            if (string.IsNullOrEmpty(requestIp) || requestIp == "::1") requestIp = "192.30.252.128";
            _requestIp = requestIp;
            _logger = logger;

            _client = new RestClient(new RestClientOptions(baseUrl)
            {
                UserAgent = WebHelper.UserAgent
            });

            ServicePointManager.ServerCertificateValidationCallback += delegate
            {
                return true; // **** Always accept
            };
        }

        public abstract RestRequest BuildRequest(string query, int resultsCount);

        public virtual Task<IEnumerable<ImageSearchResult>> PerformImageSearchAsync(string query, int resultsCount)
        {
            throw new NotImplementedException();
        }
    }
}
