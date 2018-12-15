using Roadie.Library.MetaData.Audio;
using Roadie.Library.SearchEngines.MetaData;
using System.Threading.Tasks;

namespace Roadie.Library.Engines
{
    public interface IReleaseLookupEngine
    {
        IReleaseSearchEngine DiscogsReleaseSearchEngine { get; }
        IReleaseSearchEngine ITunesReleaseSearchEngine { get; }
        IReleaseSearchEngine LastFmReleaseSearchEngine { get; }
        IReleaseSearchEngine MusicBrainzReleaseSearchEngine { get; }
        IReleaseSearchEngine SpotifyReleaseSearchEngine { get; }
        IReleaseSearchEngine WikipediaReleaseSearchEngine { get; }

        Task<OperationResult<Data.Release>> GetByName(Data.Artist artist, AudioMetaData metaData, bool doFindIfNotInDatabase = false, bool doAddTracksInDatabase = false, int? submissionId = null);
        Task<OperationResult<Data.Release>> Add(Data.Release release, bool doAddTracksInDatabase = false);
        Task<OperationResult<Data.Release>> PerformMetaDataProvidersReleaseSearch(AudioMetaData metaData, string artistFolder = null, int? submissionId = null);
    }
}