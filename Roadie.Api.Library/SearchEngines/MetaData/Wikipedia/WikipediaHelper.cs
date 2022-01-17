using Microsoft.Extensions.Logging;
using RestSharp;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.MetaData;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.MetaData.Wikipedia
{
    public class WikipediaHelper : MetaDataProviderBase, IWikipediaHelper
    {
        private IHttpEncoder HttpEncoder { get; }

        public WikipediaHelper(
            IRoadieSettings configuration,
            ICacheManager cacheManager,
            ILogger<WikipediaHelper> logger,
            IHttpEncoder httpEncoder,
            IHttpClientFactory httpClientFactory)
            : base(configuration, cacheManager, logger, httpClientFactory)
        {
            HttpEncoder = httpEncoder;
        }

        public async Task<OperationResult<IEnumerable<ArtistSearchResult>>> PerformArtistSearchAsync(string query, int resultsCount)
        {
            if(string.IsNullOrEmpty(query) || resultsCount == 0)
            {
                return new OperationResult<IEnumerable<ArtistSearchResult>>();
            }
            var client = new RestClient("https://en.wikipedia.org/w/api.php?format=xml&action=query&redirects=1&prop=extracts&exintro=&explaintext=&titles=" + HttpEncoder.UrlEncode(query ?? string.Empty));
            var request = new RestRequest();
            request.Method = Method.Get;
            var response = await client.ExecuteAsync<api>(request).ConfigureAwait(false);
            ArtistSearchResult data = null;
            if (response?.Data?.query?.pages?.Any() ?? false)
            {
                var bio = response?.Data?.query?.pages.FirstOrDefault()?.extract;
                if (bio != null)
                {
                    data = new ArtistSearchResult
                    {
                        Bio = response.Data.query.pages.First().extract
                    };
                }
            }
            return new OperationResult<IEnumerable<ArtistSearchResult>>
            {
                IsSuccess = data != null,
                Data = data != null ? new[] { data } : null
            };
        }

        public async Task<OperationResult<IEnumerable<ReleaseSearchResult>>> PerformReleaseSearch(string artistName, string query, int resultsCount)
        {
            var client = new RestClient("https://en.wikipedia.org/w/api.php?format=xml&action=query&redirects=1&prop=extracts&exintro=&explaintext=&titles=" + HttpEncoder.UrlEncode(query ?? string.Empty) + " (album)");
            var request = new RestRequest();
            request.Method = Method.Get;
            var response = await client.ExecuteAsync<api>(request).ConfigureAwait(false);
            ReleaseSearchResult data = null;
            if (response?.Data?.query?.pages != null)
            {
                data = new ReleaseSearchResult
                {
                    Bio = response.Data.query.pages.First().extract
                };
            }
            return new OperationResult<IEnumerable<ReleaseSearchResult>>
            {
                IsSuccess = data != null,
                Data = new[] { data }
            };
        }
    }
}