using System.Collections.Generic;
using System.Threading.Tasks;
using Roadie.Library.Data;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.SearchEngines.MetaData;

namespace Roadie.Library.MetaData.MusicBrainz
{
    public interface IMusicBrainzProvider
    {
        bool IsEnabled { get; }

        Task<CoverArtArchivesResult> CoverArtForMusicBrainzReleaseById(string musicBrainzId);
        Task<Release> MusicBrainzReleaseById(string musicBrainzId);
        Task<IEnumerable<AudioMetaData>> MusicBrainzReleaseTracks(string artistName, string releaseTitle);
        Task<OperationResult<IEnumerable<ArtistSearchResult>>> PerformArtistSearch(string query, int resultsCount);
        Task<OperationResult<IEnumerable<ReleaseSearchResult>>> PerformReleaseSearch(string artistName, string query, int resultsCount);
        Task<Data.Release> ReleaseForMusicBrainzReleaseById(string musicBrainzId);
        Task<IEnumerable<Release>> ReleasesForArtist(string artist, string artistMusicBrainzId = null);
    }
}