using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.MetaData.LastFm;
using Roadie.Library.Models.Users;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Library.Scrobble
{
    /// <summary>
    /// LastFM Scrobbler
    /// <seealso cref="https://www.last.fm/api/scrobbling"/>
    /// </summary>
    public class LastFMScrobbler : ScrobblerIntegrationBase
    {
        private ILastFmHelper LastFmHelper { get; }

        public LastFMScrobbler(IRoadieSettings configuration, ILogger logger, data.IRoadieDbContext dbContext, ICacheManager cacheManager, ILastFmHelper lastFmHelper)
            : base(configuration, logger, dbContext, cacheManager)
        {
            LastFmHelper = lastFmHelper;
        }

        /// <summary>
        /// Send a Now Playing Request.
        /// <remark>The "Now Playing" service lets a client notify Last.fm that a user has started listening to a track. This does not affect a user's charts, but will feature the current track on their profile page, along with an indication of what music player they're using.</remark>
        /// </summary>
        public override async Task<OperationResult<bool>> NowPlaying(User roadieUser, ScrobbleInfo scrobble) => await LastFmHelper.NowPlaying(roadieUser, scrobble);

        /// <summary>
        /// Send a Scrobble Request
        /// <remark>The scrobble service lets a client add a track-play to a user's profile. This data is used to show a user's listening history and generate personalised charts and recommendations (and more).</remark>
        /// </summary>
        public override async Task<OperationResult<bool>> Scrobble(User roadieUser, ScrobbleInfo scrobble) => await LastFmHelper.Scrobble(roadieUser, scrobble);
    }
}