using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Models.Users;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Library.Scrobble
{
    public class RoadieScrobbler : ScrobblerIntegrationBase
    {
        public RoadieScrobbler(IRoadieSettings configuration, ILogger logger, data.IRoadieDbContext dbContext, ICacheManager cacheManager)
            : base(configuration, logger, dbContext, cacheManager)
        {
        }

        /// <summary>
        /// The user has started playing a track.
        /// </summary>
        public override async Task<OperationResult<bool>> NowPlaying(User roadieUser, ScrobbleInfo scrobble) => await ScrobbleAction(roadieUser, scrobble, true);

        /// <summary>
        /// The user has played a track.
        /// </summary>
        public override async Task<OperationResult<bool>> Scrobble(User roadieUser, ScrobbleInfo scrobble) => await ScrobbleAction(roadieUser, scrobble, false);

        private async Task<OperationResult<bool>> ScrobbleAction(User roadieUser, ScrobbleInfo scrobble, bool isNowPlaying)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var track = this.DbContext.Tracks
                                         .Include(x => x.ReleaseMedia)
                                         .Include(x => x.ReleaseMedia.Release)
                                         .Include(x => x.ReleaseMedia.Release.Artist)
                                         .Include(x => x.TrackArtist)
                                        .FirstOrDefault(x => x.RoadieId == scrobble.TrackId);
                if (track == null)
                {
                    return new OperationResult<bool>($"Scrobble: Unable To Find Track [{ scrobble.TrackId }]");
                }
                if (!track.IsValid)
                {
                    return new OperationResult<bool>($"Scrobble: Invalid Track. Track Id [{scrobble.TrackId}], FilePath [{track.FilePath}], Filename [{track.FileName}]");
                }
                data.UserTrack userTrack = null;
                var now = DateTime.UtcNow;
                var success = false;
                try
                {
                    // Only create (or update) a user track activity record when the user has played the entire track.
                    if (!isNowPlaying)
                    {
                        var user = this.DbContext.Users.FirstOrDefault(x => x.RoadieId == roadieUser.UserId);
                        userTrack = this.DbContext.UserTracks.FirstOrDefault(x => x.UserId == roadieUser.Id && x.TrackId == track.Id);
                        if (userTrack == null)
                        {
                            userTrack = new data.UserTrack(now)
                            {
                                UserId = user.Id,
                                TrackId = track.Id
                            };
                            this.DbContext.UserTracks.Add(userTrack);
                        }
                        userTrack.LastPlayed = now;
                        userTrack.PlayedCount = (userTrack.PlayedCount ?? 0) + 1;
                        this.CacheManager.ClearRegion(user.CacheRegion);
                        await this.DbContext.SaveChangesAsync();
                    }
                    success = true;
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, $"Error in Scrobble, Creating UserTrack: User `{ roadieUser}` TrackId [{ track.Id }");
                }
                sw.Stop();
                this.Logger.LogInformation($"RoadieScrobbler: RoadieUser `{ roadieUser }` { (isNowPlaying ? "NowPlaying" : "Scrobble") } `{ scrobble }`");
                return new OperationResult<bool>
                {
                    Data = success,
                    IsSuccess = userTrack != null,
                    OperationTime = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, $"Scrobble RoadieUser `{ roadieUser }` Scrobble `{ scrobble }`");
            }
            return new OperationResult<bool>();
        }
    }
}