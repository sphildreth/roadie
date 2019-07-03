using Roadie.Library.Data;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.SearchEngines.MetaData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.Engines
{
    public interface IArtistLookupEngine
    {
        IEnumerable<int> AddedArtistIds { get; }
        IArtistSearchEngine DiscogsArtistSearchEngine { get; }
        IArtistSearchEngine ITunesArtistSearchEngine { get; }
        IArtistSearchEngine LastFmArtistSearchEngine { get; }
        IArtistSearchEngine MusicBrainzArtistSearchEngine { get; }
        IArtistSearchEngine SpotifyArtistSearchEngine { get; }
        IArtistSearchEngine WikipediaArtistSearchEngine { get; }

        Task<OperationResult<Artist>> Add(Artist artist);

        Artist DatabaseQueryForArtistName(string name, string sortName = null);

        Task<OperationResult<Artist>> GetByName(AudioMetaData metaData, bool doFindIfNotInDatabase = false);

        Task<OperationResult<Artist>> PerformMetaDataProvidersArtistSearch(AudioMetaData metaData);
    }
}