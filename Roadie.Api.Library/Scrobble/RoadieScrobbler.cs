using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Library.Scrobble
{
    public class RoadieScrobbler : ScrobblerIntegrationBase, IRoadieScrobbler
    {
        public RoadieScrobbler(IRoadieSettings configuration, ILogger<RoadieScrobbler> logger, data.IRoadieDbContext dbContext,
            ICacheManager cacheManager, IHttpContext httpContext)
            : base(configuration, logger, dbContext, cacheManager, httpContext)
        {
        }

        /// <summary>
        ///     For Roadie we only add a user play on the full scrobble event, otherwise we get double track play numbers.
        /// </summary>
        public override async Task<OperationResult<bool>> NowPlaying(User roadieUser, ScrobbleInfo scrobble)
        {
            return await Task.FromResult(new OperationResult<bool>
            {
                Data = true,
                IsSuccess = true
            });
        }

        /// <summary>
        ///     The user has played a track.
        /// </summary>
        public override async Task<OperationResult<bool>> Scrobble(User roadieUser, ScrobbleInfo scrobble)
        {
            try
            {
                // If less than half of duration then do nothing
                if (scrobble.ElapsedTimeOfTrackPlayed.TotalSeconds < scrobble.TrackDuration.TotalSeconds / 2)
                    return new OperationResult<bool>
                    {
                        Data = true,
                        IsSuccess = true
                    };

                var sw = Stopwatch.StartNew();
                var track = DbContext.Tracks
                    .Include(x => x.ReleaseMedia)
                    .Include(x => x.ReleaseMedia.Release)
                    .Include(x => x.ReleaseMedia.Release.Artist)
                    .Include(x => x.TrackArtist)
                    .FirstOrDefault(x => x.RoadieId == scrobble.TrackId);
                if (track == null)
                    return new OperationResult<bool>($"Scrobble: Unable To Find Track [{scrobble.TrackId}]");
                if (!track.IsValid)
                    return new OperationResult<bool>(
                        $"Scrobble: Invalid Track. Track Id [{scrobble.TrackId}], FilePath [{track.FilePath}], Filename [{track.FileName}]");
                data.UserTrack userTrack = null;
                var now = DateTime.UtcNow;
                var success = false;
                try
                {
                    var user = DbContext.Users.FirstOrDefault(x => x.RoadieId == roadieUser.UserId);
                    userTrack = DbContext.UserTracks.FirstOrDefault(x => x.UserId == user.Id && x.TrackId == track.Id);
                    if (userTrack == null)
                    {
                        userTrack = new data.UserTrack(now)
                        {
                            UserId = user.Id,
                            TrackId = track.Id
                        };
                        DbContext.UserTracks.Add(userTrack);
                    }

                    userTrack.LastPlayed = now;
                    userTrack.PlayedCount = (userTrack.PlayedCount ?? 0) + 1;

                    track.PlayedCount = (track.PlayedCount ?? 0) + 1;
                    track.LastPlayed = now;

                    var release = DbContext.Releases.Include(x => x.Artist)
                        .FirstOrDefault(x => x.RoadieId == track.ReleaseMedia.Release.RoadieId);
                    release.LastPlayed = now;
                    release.PlayedCount = (release.PlayedCount ?? 0) + 1;

                    var artist = DbContext.Artists.FirstOrDefault(x => x.RoadieId == release.Artist.RoadieId);
                    artist.LastPlayed = now;
                    artist.PlayedCount = (artist.PlayedCount ?? 0) + 1;

                    data.Artist trackArtist = null;
                    if (track.ArtistId.HasValue)
                    {
                        trackArtist = DbContext.Artists.FirstOrDefault(x => x.Id == track.ArtistId);
                        if (trackArtist != null)
                        {
                            trackArtist.LastPlayed = now;
                            trackArtist.PlayedCount = (trackArtist.PlayedCount ?? 0) + 1;
                            CacheManager.ClearRegion(trackArtist.CacheRegion);
                        }
                        else
                        {
                            Logger.LogWarning($"Invalid Track Artist [{ track.ArtistId }] on Track [{ track.Id }]");
                        }
                    }

                    await DbContext.SaveChangesAsync();

                    CacheManager.ClearRegion(track.CacheRegion);
                    CacheManager.ClearRegion(track.ReleaseMedia.Release.CacheRegion);
                    CacheManager.ClearRegion(track.ReleaseMedia.Release.Artist.CacheRegion);
                    CacheManager.ClearRegion(user.CacheRegion);

                    success = true;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex,
                        $"Error in Scrobble, Creating UserTrack: User `{roadieUser}` TrackId [{track.Id}");
                }

                sw.Stop();
                Logger.LogInformation($"RoadieScrobbler: RoadieUser `{roadieUser}` Scrobble `{scrobble}`");
                return new OperationResult<bool>
                {
                    Data = success,
                    IsSuccess = userTrack != null,
                    OperationTime = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Scrobble RoadieUser `{roadieUser}` Scrobble `{scrobble}`");
            }

            return new OperationResult<bool>();
        }
    }
}