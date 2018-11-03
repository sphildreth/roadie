using RestSharp;
using Roadie.Library.SearchEngines.Imaging.BingModels;
using Roadie.Library.Utility;
using Roadie.Library.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Roadie.Library.Setttings;

namespace Roadie.Library.SearchEngines.Imaging
{
    /// <summary>
    /// https://msdn.microsoft.com/en-us/library/dn760791(v=bsynd.50).aspx
    /// </summary>
    public class BingImageSearchEngine : ImageSearchEngineBase
    {

        public BingImageSearchEngine(IConfiguration configuration, ILogger loggingService, string requestIp = null, string referrer = null)
            : base(configuration, loggingService, "https://api.cognitive.microsoft.com", requestIp, referrer)
        {
            this._apiKey = configuration.GetValue<List<ApiKey>>("ApiKeys", new List<ApiKey>()).FirstOrDefault(x => x.ApiName == "BingImageSearch") ?? new ApiKey();
        }

        public override async Task<IEnumerable<ImageSearchResult>> PerformImageSearch(string query, int resultsCount)
        {
            var request = this.BuildRequest(query, resultsCount);

            var response = await _client.ExecuteTaskAsync<BingImageResult>(request);

            if (response.ResponseStatus == ResponseStatus.Error)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new AuthenticationException("Api Key is not correct");
                }
                throw new Exception(string.Format("Request Error Message: {0}. Content: {1}.", response.ErrorMessage, response.Content));
            }
            if (response.Data == null || response.Data.value == null)
            {
                this.LoggingService.Warning("Response Is Null on PerformImageSearch [" + Newtonsoft.Json.JsonConvert.SerializeObject(response) + "]");
                return null;
            }
            return response.Data.value.Select(x => new ImageSearchResult
            {
               Width = (x.width ?? 0).ToString(),
               Height =  (x.height ?? 0).ToString(),
               MediaUrl = x.contentUrl,
               Title = x.name
            }).ToArray();
        }

        public override RestRequest BuildRequest(string query, int resultsCount)
        {
            var request = new RestRequest
            {
                Resource = "/bing/v5.0/images/search",
                Method = Method.GET,
                RequestFormat = DataFormat.Json
            };

            request.AddHeader("Ocp-Apim-Subscription-Key", this.ApiKey.Key);

            request.AddParameter(new Parameter
            {
                Name = "count",
                Value = resultsCount > 0 ? resultsCount : 10,
                Type = ParameterType.GetOrPost
            });

            request.AddParameter(new Parameter
            {
                Name = "safeSearch",
                Value = "Off",
                Type = ParameterType.GetOrPost
            });

            request.AddParameter(new Parameter
            {
                Name = "aspect",
                Value = "Square",
                Type = ParameterType.GetOrPost
            });

            request.AddParameter(new Parameter
            {
                Name = "q",
                Value = string.Format("'{0}'", query.Trim()),
                Type = ParameterType.GetOrPost
            });

            return request;
        }
    }
}