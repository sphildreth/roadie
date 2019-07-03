using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.MetaData;
using Roadie.Library.SearchEngines.Imaging;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.MetaData.iTunes
{
    public class iTunesHelper : MetaDataProviderBase
    {
        private readonly ITunesSearchEngine _iTunesSearchEngine;

        public override bool IsEnabled => Configuration.Integrations.ITunesProviderEnabled;

        public iTunesHelper(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger)
                    : base(configuration, cacheManager, logger)
        {
            _iTunesSearchEngine = new ITunesSearchEngine(configuration, cacheManager, logger);
        }

        public async Task<OperationResult<ArtistSearchResult>> SearchForArtist(string artistName)
        {
            var r = await _iTunesSearchEngine.PerformArtistSearch(artistName, 1);
            return new OperationResult<ArtistSearchResult>
            {
                Data = r.Data != null ? r.Data.First() : null,
                IsSuccess = r.Data != null
            };
        }
    }
}