using Microsoft.Extensions.Logging;
using RestSharp;
using Roadie.Library.Configuration;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.Imaging
{
    public abstract class ImageSearchEngineBase : IImageSearchEngine
    {
        protected readonly RestClient _client = null;
        protected readonly IRoadieSettings _configuratio = null;
        protected readonly string _referrer = null;
        protected readonly string _requestIp = null;
        protected IApiKey _apiKey = null;
        protected ILogger _logger = null;

        protected IApiKey ApiKey
        {
            get
            {
                return this._apiKey;
            }
        }

        protected IRoadieSettings Configuration
        {
            get
            {
                return this._configuratio;
            }
        }

        protected ILogger Logger
        {
            get
            {
                return this._logger;
            }
        }

        public ImageSearchEngineBase(IRoadieSettings configuration, ILogger logger, string baseUrl, string requestIp = null, string referrer = null)
        {
            this._configuratio = configuration;
            if (string.IsNullOrEmpty(referrer) || referrer.StartsWith("http://localhost"))
            {
                referrer = "http://github.com/sphildreth/Roadie";
            }
            this._referrer = referrer;
            if (string.IsNullOrEmpty(requestIp) || requestIp == "::1")
            {
                requestIp = "192.30.252.128";
            }
            this._requestIp = requestIp;
            this._logger = logger;

            this._client = new RestClient(baseUrl)
            {
                UserAgent = WebHelper.UserAgent
            };

            System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                        System.Security.Cryptography.X509Certificates.X509Chain chain,
                        System.Net.Security.SslPolicyErrors sslPolicyErrors)
            {
                return true; // **** Always accept
            };
        }

        public abstract RestRequest BuildRequest(string query, int resultsCount);

        public virtual Task<IEnumerable<ImageSearchResult>> PerformImageSearch(string query, int resultsCount)
        {
            throw new NotImplementedException();
        }
    }
}