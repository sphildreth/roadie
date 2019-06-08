using System.Collections.Generic;
using System.Threading.Tasks;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.Scrobble;
using Roadie.Library.SearchEngines.MetaData;

namespace Roadie.Library.MetaData.LastFm
{
    public interface ILastFmHelper : IScrobblerIntegration
    {
        bool IsEnabled { get; }

        Task<OperationResult<IEnumerable<ArtistSearchResult>>> PerformArtistSearch(string query, int resultsCount);
        Task<OperationResult<IEnumerable<ReleaseSearchResult>>> PerformReleaseSearch(string artistName, string query, int resultsCount);
        Task<IEnumerable<AudioMetaData>> TracksForRelease(string artist, string Release);
        Task<OperationResult<string>> GetSessionKeyForUserToken(string token);

    }
}