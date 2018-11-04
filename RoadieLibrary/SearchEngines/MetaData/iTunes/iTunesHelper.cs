using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Logging;
using Roadie.Library.MetaData;
using Roadie.Library.SearchEngines.Imaging;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.SearchEngines.MetaData.iTunes
{
    public class iTunesHelper : MetaDataProviderBase
    {
        private readonly ITunesSearchEngine _iTunesSearchEngine = null;

        public override bool IsEnabled
        {
            get
            {
                return this.Configuration.Integrations.ITunesProviderEnabled;
            }
        }

        public iTunesHelper(IRoadieSettings configuration, ICacheManager cacheManager, ILogger loggingService)
            : base(configuration, cacheManager, loggingService)
        {
            this._iTunesSearchEngine = new ITunesSearchEngine(configuration, cacheManager, loggingService);
        }

        public async Task<OperationResult<ArtistSearchResult>> SearchForArtist(string artistName)
        {
            var r = await this._iTunesSearchEngine.PerformArtistSearch(artistName, 1);
            return new OperationResult<ArtistSearchResult>
            {
                Data = r.Data != null ? r.Data.First() : null,
                IsSuccess = r.Data != null
            };
        }
    }
}