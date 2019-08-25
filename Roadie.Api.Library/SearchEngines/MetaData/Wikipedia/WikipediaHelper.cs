using Microsoft.Extensions.Logging;
using RestSharp;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.MetaData;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.MetaData.Wikipedia
{
    public class WikipediaHelper : MetaDataProviderBase, IWikipediaHelper
    {
        private IHttpEncoder HttpEncoder { get; }

        public WikipediaHelper(IRoadieSettings configuration, ICacheManager cacheManager, ILogger<WikipediaHelper> logger,
                    IHttpEncoder httpEncoder)
            : base(configuration, cacheManager, logger)
        {
            HttpEncoder = httpEncoder;
        }

        public Task<OperationResult<IEnumerable<ArtistSearchResult>>> PerformArtistSearch(string query, int resultsCount)
        {
            var tcs = new TaskCompletionSource<OperationResult<IEnumerable<ArtistSearchResult>>>();
            var client = new RestClient("https://en.wikipedia.org/w/api.php?format=xml&action=query&redirects=1&prop=extracts&exintro=&explaintext=&titles=" + HttpEncoder.UrlEncode(query ?? string.Empty));
            var request = new RestRequest(Method.GET);
            client.ExecuteAsync<api>(request, response =>
            {
                ArtistSearchResult data = null;
                if (response?.Data?.query?.pages?.Any() ?? false)
                {
                    data = new ArtistSearchResult
                    {
                        Bio = response.Data.query.pages.First().extract
                    };
                }
                tcs.SetResult(new OperationResult<IEnumerable<ArtistSearchResult>>
                {
                    IsSuccess = data != null,
                    Data = data != null ? new[] { data } : null
                });
            });
            return tcs.Task;
        }

        public Task<OperationResult<IEnumerable<ReleaseSearchResult>>> PerformReleaseSearch(string artistName,
            string query, int resultsCount)
        {
            var tcs = new TaskCompletionSource<OperationResult<IEnumerable<ReleaseSearchResult>>>();

            var client = new RestClient("https://en.wikipedia.org/w/api.php?format=xml&action=query&redirects=1&prop=extracts&exintro=&explaintext=&titles=" + HttpEncoder.UrlEncode(query ?? string.Empty) + " (album)");
            var request = new RestRequest(Method.GET);
            client.ExecuteAsync<api>(request, response =>
            {
                ReleaseSearchResult data = null;
                if (response?.Data?.query?.pages != null)
                {
                    data = new ReleaseSearchResult
                    {
                        Bio = response.Data.query.pages.First().extract
                    };
                }
                tcs.SetResult(new OperationResult<IEnumerable<ReleaseSearchResult>>
                {
                    IsSuccess = data != null,
                    Data = new[] { data }
                });
            });
            return tcs.Task;
        }
    }
}