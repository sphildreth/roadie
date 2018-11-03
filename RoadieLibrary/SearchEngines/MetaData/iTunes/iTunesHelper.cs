using Roadie.Library.Caching;
using Roadie.Library.MetaData;
using Roadie.Library.SearchEngines.Imaging;
using Roadie.Library.Logging;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Roadie.Library.SearchEngines.MetaData.iTunes
{
    public class iTunesHelper : MetaDataProviderBase
    {
        private readonly ITunesSearchEngine _iTunesSearchEngine = null;

        public override bool IsEnabled
        {
            get
            {
                return this.Configuration.GetValue<bool>("Integrations:ITunesProviderEnabled", true);
            }
        }

        public iTunesHelper(IConfiguration configuration, ICacheManager cacheManager, ILogger loggingService) 
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