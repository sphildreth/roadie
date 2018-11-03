using Roadie.Library.Caching;
using RestSharp;
using Roadie.Library.MetaData;
using Roadie.Library.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Roadie.Library.Encoding;

namespace Roadie.Library.SearchEngines.MetaData.Wikipedia
{
    public class WikipediaHelper : MetaDataProviderBase, IArtistSearchEngine, IReleaseSearchEngine
    {
        private readonly IHttpEncoder _httpEncoder = null;

        private IHttpEncoder HttpEncoder
        {
            get
            {
                return this._httpEncoder;
            }
        }

        public WikipediaHelper(IConfiguration configuration, ICacheManager cacheManager, ILogger logger, IHttpEncoder httpEncoder) 
            : base(configuration, cacheManager, logger)
        {
            this._httpEncoder = httpEncoder;
        }

        public Task<OperationResult<IEnumerable<ArtistSearchResult>>> PerformArtistSearch(string query, int resultsCount)
        {
            var tcs = new TaskCompletionSource<OperationResult<IEnumerable<ArtistSearchResult>>>();
            var client = new RestClient("https://en.wikipedia.org/w/api.php?format=xml&action=query&redirects=1&prop=extracts&exintro=&explaintext=&titles=" + this.HttpEncoder.UrlEncode(query));
            var request = new RestRequest(Method.GET);
            client.ExecuteAsync<api>(request, (response) =>
            {
                ArtistSearchResult data = null;
                if (response != null && response.Data != null && response.Data.query != null && response.Data.query.pages != null)
                {
                    data = new ArtistSearchResult
                    {
                        Bio = response.Data.query.pages.First().extract
                    };
                }
                tcs.SetResult(new OperationResult<IEnumerable<ArtistSearchResult>>
                {
                    IsSuccess = data != null,
                    Data = new ArtistSearchResult[] { data }
                });
            });
            return tcs.Task;
        }

        public Task<OperationResult<IEnumerable<ReleaseSearchResult>>> PerformReleaseSearch(string artistName, string query, int resultsCount)
        {
            var tcs = new TaskCompletionSource<OperationResult<IEnumerable<ReleaseSearchResult>>>();

            var client = new RestClient("https://en.wikipedia.org/w/api.php?format=xml&action=query&redirects=1&prop=extracts&exintro=&explaintext=&titles=" + this.HttpEncoder.UrlEncode(query) + " (album)");
            var request = new RestRequest(Method.GET);
            client.ExecuteAsync<api>(request, (response) =>
            {
                ReleaseSearchResult data = null;
                if (response.Data != null)
                {
                    data = new ReleaseSearchResult
                    {
                        Bio = response.Data.query.pages.First().extract
                    };
                }
                tcs.SetResult(new OperationResult<IEnumerable<ReleaseSearchResult>>
                {
                    IsSuccess = data != null,
                    Data = new ReleaseSearchResult[] { data }
                });
            });
            return tcs.Task;
        }
    }
}