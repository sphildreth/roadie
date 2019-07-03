using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Imaging;
using Roadie.Library.Utility;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.Imaging
{
    public class ImageSearchManager : IImageSearchManager
    {
        private readonly IImageSearchEngine _bingSearchEngine;
        private readonly IImageSearchEngine _itunesSearchEngine;

        private int DefaultResultsCount => 10;

        public ImageSearchManager(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger,
                    string requestIp = null, string referrer = null)
        {
            _bingSearchEngine = new BingImageSearchEngine(configuration, logger, requestIp, referrer);
            _itunesSearchEngine = new ITunesSearchEngine(configuration, cacheManager, logger, requestIp, referrer);
        }

        public async Task<IEnumerable<ImageSearchResult>> ImageSearch(string query, int? resultsCount = null)
        {
            var count = resultsCount ?? DefaultResultsCount;
            var result = new List<ImageSearchResult>();

            if (WebHelper.IsStringUrl(query))
            {
                var s = ImageHelper.ImageSearchResultForImageUrl(query);
                if (s != null) result.Add(s);
            }

            var bingResults = await _bingSearchEngine.PerformImageSearch(query, count);
            if (bingResults != null) result.AddRange(bingResults);
            var iTunesResults = await _itunesSearchEngine.PerformImageSearch(query, count);
            if (iTunesResults != null) result.AddRange(iTunesResults);
            return result;
        }
    }
}