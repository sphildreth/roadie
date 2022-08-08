using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Imaging;
using Roadie.Library.Utility;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.Imaging
{
    public class ImageSearchManager : IImageSearchManager
    {
        private readonly IImageSearchEngine _bingSearchEngine;

        private readonly IImageSearchEngine _itunesSearchEngine;

        private readonly IHttpClientFactory _httpClientFactory;

        private IRoadieSettings Configuration { get; }

        private int DefaultResultsCount => 10;

        public ImageSearchManager(
            IRoadieSettings configuration,
            ICacheManager cacheManager,
            ILogger<ImageSearchManager> logger,
            IBingImageSearchEngine bingImageSearchEngine,
            IITunesSearchEngine iTunesSearchEngine,
            IHttpClientFactory httpClientFactory,
            string requestIp = null,
            string referrer = null)
        {
            Configuration = configuration;
            _bingSearchEngine = bingImageSearchEngine;
            _itunesSearchEngine = iTunesSearchEngine;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IEnumerable<ImageSearchResult>> ImageSearch(string query, int? resultsCount = null)
        {
            var count = resultsCount ?? DefaultResultsCount;
            var result = new List<ImageSearchResult>();
            if (WebHelper.IsStringUrl(query))
            {
                var s = await ImageHelper.ImageSearchResultForImageUrl(_httpClientFactory, query);
                if (s != null) result.Add(s);
            }
            if (Configuration.Integrations.BingImageSearchEngineEnabled)
            {
                var bingResults = await _bingSearchEngine.PerformImageSearchAsync(query, count).ConfigureAwait(false);
                if (bingResults != null)
                {
                    result.AddRange(bingResults);
                }
            }
            if (Configuration.Integrations.ITunesProviderEnabled)
            {
                var iTunesResults = await _itunesSearchEngine.PerformImageSearchAsync(query, count).ConfigureAwait(false);
                if (iTunesResults != null)
                {
                    result.AddRange(iTunesResults);
                }
            }
            return result;
        }
    }
}
