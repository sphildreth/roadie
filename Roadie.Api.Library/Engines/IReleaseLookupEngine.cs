using Roadie.Library.Data;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.SearchEngines.MetaData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.Engines
{
    public interface IReleaseLookupEngine
    {
        IEnumerable<int> AddedReleaseIds { get; }
        IEnumerable<int> AddedTrackIds { get; }

        IReleaseSearchEngine DiscogsReleaseSearchEngine { get; }
        IReleaseSearchEngine ITunesReleaseSearchEngine { get; }
        IReleaseSearchEngine LastFmReleaseSearchEngine { get; }
        IReleaseSearchEngine MusicBrainzReleaseSearchEngine { get; }
        IReleaseSearchEngine SpotifyReleaseSearchEngine { get; }
        IReleaseSearchEngine WikipediaReleaseSearchEngine { get; }

        Task<OperationResult<Release>> Add(Release release, bool doAddTracksInDatabase = false);

        Task<OperationResult<Release>> GetByName(Artist artist, AudioMetaData metaData,
                    bool doFindIfNotInDatabase = false, bool doAddTracksInDatabase = false, int? submissionId = null);

        Task<OperationResult<Release>> PerformMetaDataProvidersReleaseSearch(AudioMetaData metaData,
            string artistFolder = null, int? submissionId = null);
    }
}