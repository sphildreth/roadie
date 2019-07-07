using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.MetaData;
using Roadie.Library.SearchEngines.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.MetaData.iTunes
{
    public class iTunesHelper : MetaDataProviderBase, IiTunesHelper
    {
        private readonly ITunesSearchEngine _iTunesSearchEngine;

        public override bool IsEnabled => Configuration.Integrations.ITunesProviderEnabled;

        public iTunesHelper(IRoadieSettings configuration, ICacheManager cacheManager, ILogger<iTunesHelper> logger, 
                            ITunesSearchEngine iTunesSearchEngine)
                    : base(configuration, cacheManager, logger)
        {
            _iTunesSearchEngine = iTunesSearchEngine;
        }

        public async Task<OperationResult<IEnumerable<ArtistSearchResult>>> PerformArtistSearch(string query, int resultsCount) => await _iTunesSearchEngine.PerformArtistSearch(query, resultsCount);

        public async Task<OperationResult<IEnumerable<ReleaseSearchResult>>> PerformReleaseSearch(string artistName, string query, int resultsCount) => await _iTunesSearchEngine.PerformReleaseSearch(artistName, query, resultsCount);
    }
}