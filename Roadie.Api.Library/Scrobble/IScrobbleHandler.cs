using Roadie.Library.Models.Users;
using System.Threading.Tasks;

namespace Roadie.Library.Scrobble
{
    public interface IScrobbleHandler
    {
        /// <summary>
        ///     When a user starts playing a track.
        /// </summary>
        Task<OperationResult<bool>> NowPlaying(User user, ScrobbleInfo scrobble);

        /// <summary>
        ///     When a user has either played more than 4 minutes of the track or the entire track.
        /// </summary>
        Task<OperationResult<bool>> Scrobble(User user, ScrobbleInfo scrobble);
    }
}