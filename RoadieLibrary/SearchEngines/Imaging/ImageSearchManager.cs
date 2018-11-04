using Roadie.Library.Caching;
using Roadie.Library.Utility;
using Roadie.Library.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Roadie.Library.Imaging;
using Roadie.Library.Encoding;
using Microsoft.Extensions.Configuration;

namespace Roadie.Library.SearchEngines.Imaging
{
    public class ImageSearchManager
    {
        private readonly IImageSearchEngine _bingSearchEngine = null;
        private readonly IImageSearchEngine _itunesSearchEngine = null;

        private int DefaultResultsCount
        {
            get
            {
                return 10;
            }
        }

        public ImageSearchManager(IConfiguration configuration, ICacheManager cacheManager, ILogger loggingService, string requestIp = null, string referrer = null)
        {
            this._bingSearchEngine = new BingImageSearchEngine(configuration, loggingService, requestIp, referrer);
            this._itunesSearchEngine = new ITunesSearchEngine(configuration, cacheManager, loggingService, requestIp, referrer);
        }

        public async Task<IEnumerable<ImageSearchResult>> ImageSearch(string query, int? resultsCount = null)
        {
            var count = resultsCount ?? this.DefaultResultsCount;
            var result = new List<ImageSearchResult>();

            if (WebHelper.IsStringUrl(query))
            {
                var s = ImageHelper.ImageSearchResultForImageUrl(query);
                if (s != null)
                {
                    result.Add(s);
                }
            }
            var bingResults = await this._bingSearchEngine.PerformImageSearch(query, count);
            if (bingResults != null)
            {
                result.AddRange(bingResults);
            }
            var iTunesResults = await this._itunesSearchEngine.PerformImageSearch(query, count);
            if (iTunesResults != null)
            {
                result.AddRange(iTunesResults);
            }
            return result;
        }
    }
}