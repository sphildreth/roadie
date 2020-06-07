using Microsoft.Extensions.Logging;
using RestSharp;
using Roadie.Library.Configuration;
using Roadie.Library.SearchEngines.Imaging.BingModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.Imaging
{
    /// <summary>
    ///     https://msdn.microsoft.com/en-us/library/dn760791(v=bsynd.50).aspx
    /// </summary>
    public class BingImageSearchEngine : ImageSearchEngineBase, IBingImageSearchEngine
    {

        public override bool IsEnabled => Configuration.Integrations.BingImageSearchEngineEnabled;

        public BingImageSearchEngine(IRoadieSettings configuration, ILogger<BingImageSearchEngine> logger, string requestIp = null, string referrer = null)
            : base(configuration, logger, "https://api.cognitive.microsoft.com", requestIp, referrer)
        {
            _apiKey = configuration.Integrations.ApiKeys.FirstOrDefault(x => x.ApiName == "BingImageSearch") ??
                      new ApiKey();
        }

        public override RestRequest BuildRequest(string query, int resultsCount)
        {
            var request = new RestRequest
            {
                Resource = "/bing/v7.0/images/search",
                Method = Method.GET,
                RequestFormat = DataFormat.Json
            };

            request.AddHeader("Ocp-Apim-Subscription-Key", ApiKey.Key);

            request.AddParameter(new Parameter("count", resultsCount > 0 ? resultsCount : 10, ParameterType.GetOrPost));
            request.AddParameter(new Parameter("safeSearch", "Off", ParameterType.GetOrPost));
            request.AddParameter(new Parameter("aspect", "Square", ParameterType.GetOrPost));
            request.AddParameter(new Parameter("q", string.Format("'{0}'", query.Trim()), ParameterType.GetOrPost));
            return request;
        }

        public override async Task<IEnumerable<ImageSearchResult>> PerformImageSearchAsync(string query, int resultsCount)
        {
            var request = BuildRequest(query, resultsCount);

            var response = await _client.ExecuteAsync<BingImageResult>(request).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new AuthenticationException("Api Key is not correct");

            if (response.ResponseStatus == ResponseStatus.Error)
                throw new Exception(string.Format("Request Error Message: {0}. Content: {1}.", response.ErrorMessage,
                    response.Content));
            if (response.Data == null || response.Data.value == null)
            {
                Logger.LogWarning("Response Is Null on PerformImageSearch [" + response.ErrorMessage + "]");
                return null;
            }

            return response.Data.value.Select(x => new ImageSearchResult
            {
                Width = (x.width ?? 0).ToString(),
                Height = (x.height ?? 0).ToString(),
                MediaUrl = x.contentUrl,
                Title = x.name
            }).ToArray();
        }
    }
}