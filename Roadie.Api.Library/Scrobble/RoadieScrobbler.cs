using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Library.Scrobble
{
    public class RoadieScrobbler : ScrobblerIntegrationBase, IRoadieScrobbler
    {
        public override int SortOrder => 99; // Ensure that the Roadie Scrobbler is last due to heavy database and cache manipulation

        public RoadieScrobbler(
            IRoadieSettings configuration,
            ILogger logger,
            IRoadieDbContext dbContext,
            ICacheManager cacheManager)
            : base(configuration, logger, dbContext, cacheManager, null)
        {
        }

        public RoadieScrobbler(
            IRoadieSettings configuration,
            ILogger<RoadieScrobbler> logger,
            IRoadieDbContext dbContext,
            ICacheManager cacheManager,
            IHttpContext httpContext)
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
            }).ConfigureAwait(false);
        }

        /// <summary>
        ///     The user has played a track.
        /// </summary>
        public override async Task<OperationResult<bool>> Scrobble(User roadieUser, ScrobbleInfo scrobble)
        {
            try
            {
                // If a user and If less than half of duration then do nothing
                if (roadieUser != null &&
                    scrobble.ElapsedTimeOfTrackPlayed.TotalSeconds < scrobble.TrackDuration.TotalSeconds / 2)
                {
                    Logger.LogTrace("Skipping Scrobble, Playback did not exceed minimum elapsed time");
                    return new OperationResult<bool>
                    {
                        Data = true,
                        IsSuccess = true
                    };
                }
                var sw = Stopwatch.StartNew();
                var track = await DbContext.Tracks
                    .Include(x => x.ReleaseMedia)
                    .Include(x => x.ReleaseMedia.Release)
                    .Include(x => x.ReleaseMedia.Release.Artist)
                    .Include(x => x.TrackArtist)
                    .FirstOrDefaultAsync(x => x.RoadieId == scrobble.TrackId)
                    .ConfigureAwait(false);
                if (track == null)
                {
                    return new OperationResult<bool>($"Scrobble: Unable To Find Track [{scrobble.TrackId}]");
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
                    if (roadieUser != null)
                    {
                        // User should be cached if not then skip as probably a bad user
                        var user = CacheManager.Get<Identity.User>(Identity.User.CacheUrn(roadieUser.UserId));
                        if (user != null)
                        {
                            userTrack = await DbContext.UserTracks.FirstOrDefaultAsync(x => x.UserId == user.Id && x.TrackId == track.Id).ConfigureAwait(false);
                            if (userTrack == null)
                            {
                                userTrack = new data.UserTrack(now)
                                {
                                    UserId = user.Id,
                                    TrackId = track.Id
                                };
                                await DbContext.UserTracks.AddAsync(userTrack).ConfigureAwait(false);
                            }
                            userTrack.LastPlayed = now;
                            userTrack.PlayedCount = (userTrack.PlayedCount ?? 0) + 1;

                            CacheManager.ClearRegion(user.CacheRegion);
                        }
                    }

                    track.PlayedCount = (track.PlayedCount ?? 0) + 1;
                    track.LastPlayed = now;

                    var release = await DbContext.Releases
                                           .Include(x => x.Artist)
                                           .FirstOrDefaultAsync(x => x.RoadieId == track.ReleaseMedia.Release.RoadieId)
                                           .ConfigureAwait(false);
                    release.LastPlayed = now;
                    release.PlayedCount = (release.PlayedCount ?? 0) + 1;

                    var artist = await DbContext.Artists.FirstOrDefaultAsync(x => x.RoadieId == release.Artist.RoadieId).ConfigureAwait(false);
                    artist.LastPlayed = now;
                    artist.PlayedCount = (artist.PlayedCount ?? 0) + 1;

                    data.Artist trackArtist = null;
                    if (track.ArtistId.HasValue)
                    {
                        trackArtist = await DbContext.Artists.FirstOrDefaultAsync(x => x.Id == track.ArtistId).ConfigureAwait(false);
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

                    await DbContext.SaveChangesAsync().ConfigureAwait(false);

                    CacheManager.ClearRegion(track.CacheRegion);
                    CacheManager.ClearRegion(track.ReleaseMedia.Release.CacheRegion);
                    CacheManager.ClearRegion(track.ReleaseMedia.Release.Artist.CacheRegion);

                    success = true;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error in Scrobble, Creating UserTrack: User `{roadieUser}` TrackId [{track.Id}");
                }

                sw.Stop();
                Logger.LogInformation($"RoadieScrobbler: RoadieUser `{ (roadieUser == null ? "None" : roadieUser.ToString()) }` Scrobble `{scrobble}`");
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
