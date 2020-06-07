using Roadie.Library.MetaData.Audio;
using Roadie.Library.SearchEngines.MetaData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.MetaData.MusicBrainz
{
    public interface IMusicBrainzProvider : IArtistSearchEngine, IReleaseSearchEngine
    {
       Task<IEnumerable<AudioMetaData>> MusicBrainzReleaseTracksAsync(string artistName, string releaseTitle);
    }
}