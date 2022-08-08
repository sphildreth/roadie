using Roadie.Library.MetaData.Audio;
using Roadie.Library.Scrobble;
using Roadie.Library.SearchEngines.MetaData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.MetaData.LastFm
{
    public interface ILastFmHelper : IScrobblerIntegration, IArtistSearchEngine, IReleaseSearchEngine
    {
        Task<OperationResult<string>> GetSessionKeyForUserToken(string token);

        Task<IEnumerable<AudioMetaData>> TracksForReleaseAsync(string artist, string Release);
    }
}
