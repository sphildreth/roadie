using Roadie.Library.MetaData.Audio;
using Roadie.Library.SearchEngines.MetaData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.MetaData.MusicBrainz
{
    public interface IMusicBrainzProvider : IArtistSearchEngine, IReleaseSearchEngine
    {
        Task<CoverArtArchivesResult> CoverArtForMusicBrainzReleaseById(string musicBrainzId);

        Task<Release> MusicBrainzReleaseById(string musicBrainzId);

        Task<IEnumerable<AudioMetaData>> MusicBrainzReleaseTracks(string artistName, string releaseTitle);

        Task<Data.Release> ReleaseForMusicBrainzReleaseById(string musicBrainzId);

        Task<IEnumerable<Release>> ReleasesForArtist(string artist, string artistMusicBrainzId = null);
    }
}