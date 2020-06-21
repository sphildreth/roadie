using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Identity;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public abstract class ServiceBase
    {
        public static string TrackTokenSalt = "B0246908-FBD6-4E12-A96C-AF5B086115B3";

        protected readonly ICacheManager _cacheManager;

        protected readonly IRoadieSettings _configuration;

        protected readonly IRoadieDbContext _dbContext;

        protected readonly IHttpContext _httpContext;

        protected readonly IHttpEncoder _httpEncoder;

        protected readonly ILogger _logger;

        private static readonly Lazy<Hashids> hashIds = new Lazy<Hashids>(() =>
                                                        {
                                                            return new Hashids(TrackTokenSalt);
                                                        });

        protected ICacheManager CacheManager => _cacheManager;

        protected IRoadieSettings Configuration => _configuration;

        protected IRoadieDbContext DbContext => _dbContext;

        protected IHttpContext HttpContext => _httpContext;

        protected IHttpEncoder HttpEncoder => _httpEncoder;

        protected ILogger Logger => _logger;

        public ServiceBase(IRoadieSettings configuration, IHttpEncoder httpEncoder, IRoadieDbContext context,
                           ICacheManager cacheManager, ILogger logger, IHttpContext httpContext)
        {
            _configuration = configuration;
            _httpEncoder = httpEncoder;
            _dbContext = context;
            _cacheManager = cacheManager;
            _logger = logger;
            _httpContext = httpContext;
        }

        public static bool ConfirmTrackPlayToken(User user, Guid trackRoadieId, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            return TrackPlayToken(user, trackRoadieId).Equals(token);
        }

        public static string MakeTrackPlayUrl(User user, string baseUrl, Guid trackRoadieId) => $"{baseUrl}/play/track/{user.Id}/{TrackPlayToken(user, trackRoadieId)}/{trackRoadieId}.mp3";

        public static string TrackPlayToken(User user, Guid trackId)
        {
            var trackIdPart = BitConverter.ToInt32(trackId.ToByteArray(), 6);
            if (trackIdPart < 0)
            {
                trackIdPart *= -1;
            }
            var token = hashIds.Value.Encode(user.Id, SafeParser.ToNumber<int>(user.CreatedDate.Value.ToString("DDHHmmss")), trackIdPart);
            return token;
        }

        protected async Task<data.Artist> GetArtist(string artistName)
        {
            if (string.IsNullOrEmpty(artistName))
            {
                return null;
            }
            var artistByName = await CacheManager.GetAsync(data.Artist.CacheUrnByName(artistName), async () =>
            {
               return await DbContext.Artists.FirstOrDefaultAsync(x => x.Name == artistName).ConfigureAwait(false);
            }, null).ConfigureAwait(false);
            if (artistByName == null)
            {
                return null;
            }
            return await GetArtist(artistByName.RoadieId).ConfigureAwait(false);
        }

        protected Task<data.Artist> GetArtist(Guid id)
        {
            return CacheManager.GetAsync(data.Artist.CacheUrn(id), async () =>
            {
                return await DbContext.Artists
                    .Include(x => x.Genres)
                    .Include("Genres.Genre")
                    .FirstOrDefaultAsync(x => x.RoadieId == id)
                    .ConfigureAwait(false);
            }, data.Artist.CacheRegionUrn(id));
        }

        protected Task<data.Collection> GetCollection(Guid id)
        {
            return CacheManager.GetAsync(data.Collection.CacheUrn(id), async () =>
            {
                return await DbContext.Collections.FirstOrDefaultAsync(x => x.RoadieId == id).ConfigureAwait(false);
            }, data.Collection.CacheRegionUrn(id));
        }

        protected Task<data.Genre> GetGenre(Guid id)
        {
            return CacheManager.GetAsync(data.Genre.CacheUrn(id), async () =>
            {
                return await DbContext.Genres.FirstOrDefaultAsync(x => x.RoadieId == id).ConfigureAwait(false);
            }, data.Genre.CacheRegionUrn(id));
        }

        protected Task<data.Label> GetLabel(Guid id)
        {
            return CacheManager.GetAsync(data.Label.CacheUrn(id), async () =>
            {
                return await DbContext.Labels.FirstOrDefaultAsync(x => x.RoadieId == id).ConfigureAwait(false);
            }, data.Label.CacheRegionUrn(id));
        }

        protected Task<data.Playlist> GetPlaylist(Guid id)
        {
            return CacheManager.GetAsync(data.Playlist.CacheUrn(id), async () =>
            {
                return await DbContext.Playlists
                                .Include(x => x.User)
                                .FirstOrDefaultAsync(x => x.RoadieId == id)
                                .ConfigureAwait(false);
            }, data.Playlist.CacheRegionUrn(id));
        }

        protected Task<data.Release> GetRelease(Guid id)
        {
            return CacheManager.GetAsync(data.Release.CacheUrn(id), async () =>
            {
                return await DbContext.Releases
                    .Include(x => x.Artist)
                    .Include(x => x.Genres)
                    .Include("Genres.Genre")
                    .Include(x => x.Medias)
                    .Include("Medias.Tracks")
                    .Include("Medias.Tracks.TrackArtist")
                    .FirstOrDefaultAsync(x => x.RoadieId == id).ConfigureAwait(false);
            }, data.Release.CacheRegionUrn(id));
        }

        /// <summary>
        ///     Get Track by Subsonic Id ("T:guid")
        /// </summary>
        protected async Task<data.Track> GetTrack(string id)
        {
            if (Guid.TryParse(id, out Guid trackId))
            {
                return await GetTrack(trackId).ConfigureAwait(false);
            }
            return null;
        }

        // Only read operations
        protected Task<data.Track> GetTrack(Guid id)
        {
            return CacheManager.GetAsync(data.Track.CacheUrn(id), async () =>
            {
                return await DbContext.Tracks
                    .Include(x => x.ReleaseMedia)
                    .Include(x => x.ReleaseMedia.Release)
                    .Include(x => x.ReleaseMedia.Release.Artist)
                    .Include(x => x.TrackArtist)
                    .FirstOrDefaultAsync(x => x.RoadieId == id).ConfigureAwait(false);
            }, data.Track.CacheRegionUrn(id));
        }

        protected async Task<User> GetUser(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }
            var userByUsername = await CacheManager.GetAsync(User.CacheUrnByUsername(username), () =>
            {
                return DbContext.Users.FirstOrDefaultAsync(x => x.UserName == username);
            }, null).ConfigureAwait(false);
            return await GetUser(userByUsername?.RoadieId).ConfigureAwait(false);
        }

        protected Task<User> GetUser(Guid? id)
        {
           return CacheManager.GetAsync(User.CacheUrn(id.Value), async () =>
           {
               return await DbContext.Users
                   .Include(x => x.UserRoles)
                   .Include("UserRoles.Role")
                   .Include("UserRoles.Role.RoleClaims")
                   .Include(x => x.UserClaims)
                   .Include(x => x.UserQues)
                   .Include("UserQues.Track")
                   .FirstOrDefaultAsync(x => x.RoadieId == id).ConfigureAwait(false);
           }, User.CacheRegionUrn(id.Value));
        }

        protected string MakeLastFmUrl(string artistName, string releaseTitle) => "http://www.last.fm/music/" + HttpEncoder.UrlEncode($"{artistName}/{releaseTitle}");

        protected async Task<OperationResult<short>> SetArtistRating(Guid artistId, User user, short rating)
        {
            var artist = await DbContext.Artists
                .Include(x => x.Genres)
                .Include("Genres.Genre")
                .FirstOrDefaultAsync(x => x.RoadieId == artistId)
                .ConfigureAwait(false);
            if (artist == null) return new OperationResult<short>(true, $"Invalid Artist Id [{artistId}]");
            var now = DateTime.UtcNow;
            var userArtist = await DbContext.UserArtists.FirstOrDefaultAsync(x => x.ArtistId == artist.Id && x.UserId == user.Id).ConfigureAwait(false);
            if (userArtist == null)
            {
                userArtist = new data.UserArtist
                {
                    Rating = rating,
                    UserId = user.Id,
                    ArtistId = artist.Id
                };
                DbContext.UserArtists.Add(userArtist);
            }
            else
            {
                userArtist.Rating = rating;
                userArtist.LastUpdated = now;
            }

            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            var ratings = await DbContext.UserArtists.Where(x => x.ArtistId == artist.Id && x.Rating > 0).Select(x => x.Rating).ToListAsync().ConfigureAwait(false);
            if (ratings != null && ratings.Count > 0)
            {
                artist.Rating = (short)ratings.Average(x => (decimal)x);
            }
            else
            {
                artist.Rating = 0;
            }
            artist.LastUpdated = now;
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            await UpdateArtistRank(artist.Id).ConfigureAwait(false);
            CacheManager.ClearRegion(user.CacheRegion);
            CacheManager.ClearRegion(artist.CacheRegion);

            artist = await GetArtist(artistId).ConfigureAwait(false);

            return new OperationResult<short>
            {
                IsSuccess = true,
                Data = artist.Rating ?? 0
            };
        }

        protected async Task<OperationResult<short>> SetReleaseRating(Guid releaseId, User user, short rating)
        {
            var release = await DbContext.Releases
                .Include(x => x.Artist)
                .Include(x => x.Genres)
                .Include("Genres.Genre")
                .Include(x => x.Medias)
                .Include("Medias.Tracks")
                .Include("Medias.Tracks.TrackArtist")
                .FirstOrDefaultAsync(x => x.RoadieId == releaseId).ConfigureAwait(false);
            if (release == null)
            {
                return new OperationResult<short>(true, $"Invalid Release Id [{releaseId}]");
            }
            var userRelease = await DbContext.UserReleases.FirstOrDefaultAsync(x => x.ReleaseId == release.Id && x.UserId == user.Id).ConfigureAwait(false);
            var now = DateTime.UtcNow;
            if (userRelease == null)
            {
                userRelease = new data.UserRelease
                {
                    Rating = rating,
                    UserId = user.Id,
                    ReleaseId = release.Id
                };
                await DbContext.UserReleases.AddAsync(userRelease).ConfigureAwait(false);
            }
            else
            {
                userRelease.Rating = rating;
                userRelease.LastUpdated = now;
            }

            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            var ratings = await DbContext.UserReleases.Where(x => x.ReleaseId == release.Id && x.Rating > 0).Select(x => x.Rating).ToListAsync().ConfigureAwait(false);
            if (ratings != null && ratings.Count > 0)
            {
                release.Rating = (short)ratings.Average(x => (decimal)x);
            }
            else
            {
                release.Rating = 0;
            }
            release.LastUpdated = now;
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            await UpdateReleaseRank(release.Id).ConfigureAwait(false);
            CacheManager.ClearRegion(user.CacheRegion);
            CacheManager.ClearRegion(release.CacheRegion);
            CacheManager.ClearRegion(release.Artist.CacheRegion);

            release = await GetRelease(releaseId).ConfigureAwait(false);

            return new OperationResult<short>
            {
                IsSuccess = true,
                Data = release.Rating ?? 0
            };
        }

        protected async Task<OperationResult<short>> SetTrackRating(Guid trackId, User user, short rating)
        {
            var sw = Stopwatch.StartNew();

            var track = await DbContext.Tracks
                .Include(x => x.ReleaseMedia)
                .Include(x => x.ReleaseMedia.Release)
                .Include(x => x.ReleaseMedia.Release.Artist)
                .Include(x => x.TrackArtist)
                .FirstOrDefaultAsync(x => x.RoadieId == trackId)
                .ConfigureAwait(false);
            if (track == null)
            {
                return new OperationResult<short>(true, $"Invalid Track Id [{trackId}]");
            }
            var now = DateTime.UtcNow;
            var userTrack = await DbContext.UserTracks.FirstOrDefaultAsync(x => x.TrackId == track.Id && x.UserId == user.Id).ConfigureAwait(false);
            if (userTrack == null)
            {
                userTrack = new data.UserTrack
                {
                    Rating = rating,
                    UserId = user.Id,
                    TrackId = track.Id
                };
                await DbContext.UserTracks.AddAsync(userTrack).ConfigureAwait(false);
            }
            else
            {
                userTrack.Rating = rating;
                userTrack.LastUpdated = now;
            }

            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            var ratings = await DbContext.UserTracks.Where(x => x.TrackId == track.Id && x.Rating > 0).Select(x => x.Rating).ToListAsync().ConfigureAwait(false);
            if (ratings != null && ratings.Count > 0)
            {
                track.Rating = (short)ratings.Average(x => (double)x);
            }
            else
            {
                track.Rating = 0;
            }
            track.LastUpdated = now;
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            await UpdateReleaseRank(track.ReleaseMedia.Release.Id).ConfigureAwait(false);

            CacheManager.ClearRegion(user.CacheRegion);
            CacheManager.ClearRegion(track.CacheRegion);
            CacheManager.ClearRegion(track.ReleaseMedia.Release.CacheRegion);
            CacheManager.ClearRegion(track.ReleaseMedia.Release.Artist.CacheRegion);
            if (track.TrackArtist != null)
            {
                CacheManager.ClearRegion(track.TrackArtist.CacheRegion);
            }

            sw.Stop();
            return new OperationResult<short>
            {
                IsSuccess = true,
                Data = track.Rating,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        protected async Task<OperationResult<bool>> ToggleArtistDisliked(Guid artistId, User user, bool isDisliked)
        {
            var result = false;
            try
            {
                var artist = await DbContext.Artists
                    .Include(x => x.Genres)
                    .Include("Genres.Genre")
                    .FirstOrDefaultAsync(x => x.RoadieId == artistId)
                    .ConfigureAwait(false);
                if (artist == null) return new OperationResult<bool>(true, $"Invalid Artist Id [{artistId}]");
                var userArtist = await DbContext.UserArtists.FirstOrDefaultAsync(x => x.ArtistId == artist.Id && x.UserId == user.Id).ConfigureAwait(false);
                if (userArtist == null)
                {
                    userArtist = new data.UserArtist
                    {
                        IsDisliked = isDisliked,
                        UserId = user.Id,
                        ArtistId = artist.Id
                    };
                    await DbContext.UserArtists.AddAsync(userArtist).ConfigureAwait(false);
                }
                else
                {
                    userArtist.IsDisliked = isDisliked;
                    userArtist.LastUpdated = DateTime.UtcNow;
                }

                await DbContext.SaveChangesAsync().ConfigureAwait(false);

                CacheManager.ClearRegion(user.CacheRegion);
                CacheManager.ClearRegion(artist.CacheRegion);
                result = true;
            }
            catch (DbException ex)
            {
                Logger.LogError($"ToggleArtistDisliked DbException [{ ex }]");
            }
            return new OperationResult<bool>
            {
                IsSuccess = result,
                Data = result
            };
        }

        protected async Task<OperationResult<bool>> ToggleArtistFavorite(Guid artistId, User user, bool isFavorite)
        {
            var result = false;
            try
            {
                var artist = await DbContext.Artists
                    .Include(x => x.Genres)
                    .Include("Genres.Genre")
                    .FirstOrDefaultAsync(x => x.RoadieId == artistId)
                    .ConfigureAwait(false); 
                if (artist == null) return new OperationResult<bool>(true, $"Invalid Artist Id [{artistId}]");
                var userArtist = await DbContext.UserArtists.FirstOrDefaultAsync(x => x.ArtistId == artist.Id && x.UserId == user.Id).ConfigureAwait(false);
                if (userArtist == null)
                {
                    userArtist = new data.UserArtist
                    {
                        IsFavorite = isFavorite,
                        UserId = user.Id,
                        ArtistId = artist.Id
                    };
                    await DbContext.UserArtists.AddAsync(userArtist).ConfigureAwait(false);
                }
                else
                {
                    userArtist.IsFavorite = isFavorite;
                    userArtist.LastUpdated = DateTime.UtcNow;
                }

                await DbContext.SaveChangesAsync().ConfigureAwait(false);

                CacheManager.ClearRegion(user.CacheRegion);
                CacheManager.ClearRegion(artist.CacheRegion);
                result = true;
            }
            catch (DbException ex)
            {
                Logger.LogError($"ToggleArtistFavorite DbException [{ ex }]");
            }
            return new OperationResult<bool>
            {
                IsSuccess = result,
                Data = result
            };
        }

        protected async Task<OperationResult<bool>> ToggleReleaseDisliked(Guid releaseId, User user, bool isDisliked)
        {
            var result = false;
            try
            {
                var release = await DbContext.Releases
                    .Include(x => x.Artist)
                    .Include(x => x.Genres)
                    .Include("Genres.Genre")
                    .Include(x => x.Medias)
                    .Include("Medias.Tracks")
                    .Include("Medias.Tracks.TrackArtist")
                    .FirstOrDefaultAsync(x => x.RoadieId == releaseId)
                    .ConfigureAwait(false);
                if (release == null)
                {
                    return new OperationResult<bool>(true, $"Invalid Release Id [{releaseId}]");
                }
                var userRelease = await DbContext.UserReleases.FirstOrDefaultAsync(x => x.ReleaseId == release.Id && x.UserId == user.Id).ConfigureAwait(false);
                if (userRelease == null)
                {
                    userRelease = new data.UserRelease
                    {
                        IsDisliked = isDisliked,
                        UserId = user.Id,
                        ReleaseId = release.Id
                    };
                    await DbContext.UserReleases.AddAsync(userRelease).ConfigureAwait(false);
                }
                else
                {
                    userRelease.IsDisliked = isDisliked;
                    userRelease.LastUpdated = DateTime.UtcNow;
                }

                await DbContext.SaveChangesAsync().ConfigureAwait(false);

                CacheManager.ClearRegion(user.CacheRegion);
                CacheManager.ClearRegion(release.CacheRegion);
                CacheManager.ClearRegion(release.Artist.CacheRegion);
                result = true;
            }
            catch (DbException ex)
            {
                Logger.LogError($"ToggleReleaseDisliked DbException [{ ex }]");
            }
            return new OperationResult<bool>
            {
                IsSuccess = result,
                Data = result
            };
        }

        protected async Task<OperationResult<bool>> ToggleReleaseFavorite(Guid releaseId, User user, bool isFavorite)
        {
            var result = false;
            try
            {
                var release = DbContext.Releases
                    .Include(x => x.Artist)
                    .Include(x => x.Genres)
                    .Include("Genres.Genre")
                    .Include(x => x.Medias)
                    .Include("Medias.Tracks")
                    .Include("Medias.Tracks.TrackArtist")
                    .FirstOrDefault(x => x.RoadieId == releaseId);
                if (release == null)
                {
                    return new OperationResult<bool>(true, $"Invalid Release Id [{releaseId}]");
                }
                var userRelease = await DbContext.UserReleases.FirstOrDefaultAsync(x => x.ReleaseId == release.Id && x.UserId == user.Id).ConfigureAwait(false);
                if (userRelease == null)
                {
                    userRelease = new data.UserRelease
                    {
                        IsFavorite = isFavorite,
                        UserId = user.Id,
                        ReleaseId = release.Id
                    };
                    await DbContext.UserReleases.AddAsync(userRelease).ConfigureAwait(false);
                }
                else
                {
                    userRelease.IsFavorite = isFavorite;
                    userRelease.LastUpdated = DateTime.UtcNow;
                }

                await DbContext.SaveChangesAsync().ConfigureAwait(false);

                CacheManager.ClearRegion(user.CacheRegion);
                CacheManager.ClearRegion(release.CacheRegion);
                CacheManager.ClearRegion(release.Artist.CacheRegion);
                result = true;
            }
            catch (DbException ex)
            {
                Logger.LogError($"ToggleReleaseFavorite DbException [{ ex }]");
            }
            return new OperationResult<bool>
            {
                IsSuccess = result,
                Data = result
            };
        }

        protected async Task<OperationResult<bool>> ToggleTrackDisliked(Guid trackId, User user, bool isDisliked)
        {
            var result = false;
            try
            {
                var track = await DbContext.Tracks
                    .Include(x => x.ReleaseMedia)
                    .Include(x => x.ReleaseMedia.Release)
                    .Include(x => x.ReleaseMedia.Release.Artist)
                    .Include(x => x.TrackArtist)
                    .FirstOrDefaultAsync(x => x.RoadieId == trackId)
                    .ConfigureAwait(false);
                if (track == null)
                {
                    return new OperationResult<bool>(true, $"Invalid Track Id [{trackId}]");
                }
                var userTrack = await DbContext.UserTracks.FirstOrDefaultAsync(x => x.TrackId == track.Id && x.UserId == user.Id).ConfigureAwait(false);
                if (userTrack == null)
                {
                    userTrack = new data.UserTrack
                    {
                        IsDisliked = isDisliked,
                        UserId = user.Id,
                        TrackId = track.Id
                    };
                    await DbContext.UserTracks.AddAsync(userTrack).ConfigureAwait(false);
                }
                else
                {
                    userTrack.IsDisliked = isDisliked;
                    userTrack.LastUpdated = DateTime.UtcNow;
                }

                await DbContext.SaveChangesAsync().ConfigureAwait(false);

                CacheManager.ClearRegion(user.CacheRegion);
                CacheManager.ClearRegion(track.CacheRegion);
                CacheManager.ClearRegion(track.ReleaseMedia.Release.CacheRegion);
                CacheManager.ClearRegion(track.ReleaseMedia.Release.Artist.CacheRegion);
                result = true;
            }
            catch (DbException ex)
            {
                Logger.LogError($"ToggleTrackDisliked DbException [{ ex }]");
            }
            return new OperationResult<bool>
            {
                IsSuccess = result,
                Data = result
            };
        }

        protected async Task<OperationResult<bool>> ToggleTrackFavorite(Guid trackId, User user, bool isFavorite)
        {
            var result = false;
            try
            {
                var track = await DbContext.Tracks
                    .Include(x => x.ReleaseMedia)
                    .Include(x => x.ReleaseMedia.Release)
                    .Include(x => x.ReleaseMedia.Release.Artist)
                    .Include(x => x.TrackArtist)
                    .FirstOrDefaultAsync(x => x.RoadieId == trackId)
                    .ConfigureAwait(false);
                if (track == null)
                {
                    return new OperationResult<bool>(true, $"Invalid Track Id [{trackId}]");
                }
                var userTrack = await DbContext.UserTracks.FirstOrDefaultAsync(x => x.TrackId == track.Id && x.UserId == user.Id).ConfigureAwait(false);
                if (userTrack == null)
                {
                    userTrack = new data.UserTrack
                    {
                        IsFavorite = isFavorite,
                        UserId = user.Id,
                        TrackId = track.Id
                    };
                    await DbContext.UserTracks.AddAsync(userTrack).ConfigureAwait(false);
                }
                else
                {
                    userTrack.IsFavorite = isFavorite;
                    userTrack.LastUpdated = DateTime.UtcNow;
                }

                await DbContext.SaveChangesAsync().ConfigureAwait(false);

                CacheManager.ClearRegion(user.CacheRegion);
                CacheManager.ClearRegion(track.CacheRegion);
                CacheManager.ClearRegion(track.ReleaseMedia.Release.CacheRegion);
                CacheManager.ClearRegion(track.ReleaseMedia.Release.Artist.CacheRegion);

                Logger.LogDebug($"ToggleTrackFavorite: User `{ user }`, Track `{ track }`, isFavorite: [{ isFavorite }]");

                result = true;
            }
            catch (DbException ex)
            {
                Logger.LogError($"ToggleTrackFavorite DbException [{ ex }]");
            }
            return new OperationResult<bool>
            {
                IsSuccess = result,
                Data = result
            };
        }

        protected async Task UpdateArtistCounts(int artistId, DateTime now)
        {
            var artist = await DbContext.Artists.FirstOrDefaultAsync(x => x.Id == artistId).ConfigureAwait(false);
            if (artist != null)
            {
                artist.ReleaseCount = await DbContext.Releases.Where(x => x.ArtistId == artistId).CountAsync().ConfigureAwait(false);
                artist.TrackCount = await (from r in DbContext.Releases
                                           join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                           join tr in DbContext.Tracks on rm.Id equals tr.ReleaseMediaId
                                           where tr.ArtistId == artistId || r.ArtistId == artistId
                                           select tr).CountAsync().ConfigureAwait(false);

                artist.LastUpdated = now;
                await DbContext.SaveChangesAsync().ConfigureAwait(false);
                CacheManager.ClearRegion(artist.CacheRegion);
            }
        }

        /// <summary>
        /// Update any playlist counts for any playlists with tracks for any release for this artist
        /// </summary>
        protected async Task UpdatePlaylistCountsForArtist(int artistId, DateTime now)
        {
            var playlistIds = await (from r in DbContext.Releases
                                     join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                     join tr in DbContext.Tracks on rm.Id equals tr.ReleaseMediaId
                                     join plt in DbContext.PlaylistTracks on tr.Id equals plt.TrackId
                                     where r.ArtistId == artistId
                                     select plt.PlayListId).ToListAsync().ConfigureAwait(false);
            foreach (var playlistId in playlistIds.Distinct())
            {
                await UpdatePlaylistCounts(playlistId, now).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Update any playlist counts for any playlists with tracks on this release
        /// </summary>
        protected async Task UpdatePlaylistCountsForRelease(int releaseId, DateTime now)
        {
            var playlistIds = await (from r in DbContext.Releases
                                     join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                     join tr in DbContext.Tracks on rm.Id equals tr.ReleaseMediaId
                                     join plt in DbContext.PlaylistTracks on tr.Id equals plt.TrackId
                                     where r.Id == releaseId
                                     select plt.PlayListId).ToListAsync().ConfigureAwait(false);
            foreach (var playlistId in playlistIds.Distinct())
            {
                await UpdatePlaylistCounts(playlistId, now).ConfigureAwait(false);
            }
        }


        /// <summary>
        ///     Update the counts for all artists on a release (both track and release artists)
        /// </summary>
        protected async Task UpdateArtistCountsForRelease(int releaseId, DateTime now)
        {
            var trackArtistIds = await (from r in DbContext.Releases
                                        join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                        join tr in DbContext.Tracks on rm.Id equals tr.ReleaseMediaId
                                        where r.Id == releaseId
                                        where tr.ArtistId != null
                                        select tr.ArtistId.Value).ToListAsync().ConfigureAwait(false);
            trackArtistIds.Add(DbContext.Releases.FirstOrDefault(x => x.Id == releaseId).ArtistId);
            foreach (var artistId in trackArtistIds.Distinct())
            {
                await UpdateArtistCounts(artistId, now).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Update Artist Rank
        ///     Artist Rank is a sum of the artists release ranks + artist tracks rating + artist user rating
        /// </summary>
        protected async Task UpdateArtistRank(int artistId, bool updateReleaseRanks = false)
        {
            try
            {
                var artist = await DbContext.Artists.FirstOrDefaultAsync(x => x.Id == artistId).ConfigureAwait(false);
                if (artist != null)
                {
                    if (updateReleaseRanks)
                    {
                        var artistReleaseIds = await DbContext.Releases.Where(x => x.ArtistId == artistId).Select(x => x.Id).ToArrayAsync().ConfigureAwait(false);
                        foreach (var artistReleaseId in artistReleaseIds)
                        {
                            await UpdateReleaseRank(artistReleaseId, false).ConfigureAwait(false);
                        }
                    }

                    var artistTrackAverage = (await (from t in DbContext.Tracks
                                                     join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                     join ut in DbContext.UserTracks on t.Id equals ut.TrackId
                                                     where t.ArtistId == artist.Id
                                                     select ut.Rating).Select(x => (decimal?)x).ToArrayAsync().ConfigureAwait(false)).Average();

                    var artistReleaseRatingRating = (await (from r in DbContext.Releases
                                                            join ur in DbContext.UserReleases on r.Id equals ur.ReleaseId
                                                            where r.ArtistId == artist.Id
                                                            select ur.Rating).Select(x => (decimal?)x).ToArrayAsync().ConfigureAwait(false)).Average();

                    var artistReleaseRankSum = (await (from r in DbContext.Releases
                                                       where r.ArtistId == artist.Id
                                                       select r.Rank).ToArrayAsync().ConfigureAwait(false)).Sum(x => x) ?? 0;

                    artist.Rank = SafeParser.ToNumber<decimal>(artistTrackAverage + artistReleaseRatingRating) + artistReleaseRankSum + artist.Rating;

                    await DbContext.SaveChangesAsync().ConfigureAwait(false);
                    CacheManager.ClearRegion(artist.CacheRegion);
                    Logger.LogTrace("UpdatedArtistRank For Artist `{0}`", artist);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in UpdateArtistRank ArtistId [{0}], UpdateReleaseRanks [{1}]", artistId,
                    updateReleaseRanks);
            }
        }

        /// <summary>
        ///     Find all artists involved with release and update their rank
        /// </summary>
        protected async Task UpdateArtistsRankForRelease(data.Release release)
        {
            if (release != null)
            {
                var artistsForRelease = new List<int>
                {
                    release.ArtistId
                };
                var trackArtistsForRelease = await (from t in DbContext.Tracks
                                                    join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                    where rm.ReleaseId == release.Id
                                                    where t.ArtistId.HasValue
                                                    select t.ArtistId.Value).ToArrayAsync().ConfigureAwait(false);
                artistsForRelease.AddRange(trackArtistsForRelease);
                foreach (var artistId in artistsForRelease.Distinct())
                {
                    await UpdateArtistRank(artistId).ConfigureAwait(false);
                }
            }
        }

        protected async Task UpdateLabelCounts(int labelId, DateTime now)
        {
            var label = await DbContext.Labels.FirstOrDefaultAsync(x => x.Id == labelId).ConfigureAwait(false);
            if (label != null)
            {
                label.ReleaseCount = await DbContext.ReleaseLabels.Where(x => x.LabelId == label.Id).CountAsync().ConfigureAwait(false);
                label.ArtistCount = await (from r in DbContext.Releases
                                           join rl in DbContext.ReleaseLabels on r.Id equals rl.ReleaseId
                                           join a in DbContext.Artists on r.ArtistId equals a.Id
                                           where rl.LabelId == label.Id
                                           group a by a.Id
                                           into artists
                                           select artists).Select(x => x.Key).CountAsync().ConfigureAwait(false);
                label.TrackCount = await (from r in DbContext.Releases
                                          join rl in DbContext.ReleaseLabels on r.Id equals rl.ReleaseId
                                          join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                          join t in DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                          where rl.LabelId == label.Id
                                          select t).CountAsync().ConfigureAwait(false);
                label.LastUpdated = now;
                await DbContext.SaveChangesAsync().ConfigureAwait(false);
                CacheManager.ClearRegion(label.CacheRegion);
            }
        }

        protected async Task UpdatePlaylistCounts(int playlistId, DateTime now)
        {
            var playlist = await DbContext.Playlists.FirstOrDefaultAsync(x => x.Id == playlistId).ConfigureAwait(false);
            if (playlist != null)
            {
                var playlistTracks = await DbContext.PlaylistTracks
                                                    .Include(x => x.Track)
                                                    .Include("Track.ReleaseMedia")
                                                    .Where(x => x.PlayListId == playlist.Id).ToArrayAsync().ConfigureAwait(false);
                playlist.TrackCount = (short)playlistTracks.Length;
                playlist.Duration = playlistTracks.Sum(x => x.Track.Duration);
                playlist.ReleaseCount = (short)playlistTracks.Select(x => x.Track.ReleaseMedia.ReleaseId).Distinct().Count();
                playlist.LastUpdated = now;
                await DbContext.SaveChangesAsync().ConfigureAwait(false);
                CacheManager.ClearRegion(playlist.CacheRegion);
            }
        }

        protected async Task UpdateReleaseCounts(int releaseId, DateTime now)
        {
            var release = await DbContext.Releases.FirstOrDefaultAsync(x => x.Id == releaseId).ConfigureAwait(false);
            if (release != null)
            {
                release.PlayedCount = await (from t in DbContext.Tracks
                                             join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                             where rm.ReleaseId == releaseId
                                             where t.PlayedCount.HasValue
                                             select t).SumAsync(x => x.PlayedCount).ConfigureAwait(false);
                release.Duration = await (from t in DbContext.Tracks
                                          join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                          where rm.ReleaseId == releaseId
                                          select t).SumAsync(x => x.Duration).ConfigureAwait(false);
                release.LastUpdated = now;
                await DbContext.SaveChangesAsync().ConfigureAwait(false);
                CacheManager.ClearRegion(release.CacheRegion);
            }
        }

        /// <summary>
        ///     Update Relase Rank
        ///     Release Rank Calculation = Average of Track User Ratings + (User Rating of Release / Release Track Count) +
        ///     Collection Rank Value
        /// </summary>
        protected async Task UpdateReleaseRank(int releaseId, bool updateArtistRank = true)
        {
            try
            {
                var release = await DbContext.Releases.FirstOrDefaultAsync(x => x.Id == releaseId).ConfigureAwait(false);
                if (release != null)
                {
                    var releaseTrackAverage = (await (from ut in DbContext.UserTracks
                                                      join t in DbContext.Tracks on ut.TrackId equals t.Id
                                                      join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                      where rm.ReleaseId == releaseId
                                                      select ut.Rating).Select(x => (decimal?)x).ToArrayAsync().ConfigureAwait(false)).Average();

                    var releaseUserRatingRank = release.Rating > 0 ? release.Rating / (decimal?)release.TrackCount : 0;

                    var collectionsWithRelease = from c in DbContext.Collections
                                                 join cr in DbContext.CollectionReleases on c.Id equals cr.CollectionId
                                                 where c.CollectionType != CollectionType.Chart
                                                 where cr.ReleaseId == release.Id
                                                 select new
                                                 {
                                                     c.CollectionCount,
                                                     cr.ListNumber
                                                 };

                    decimal releaseCollectionRank = 0;
                    foreach (var collectionWithRelease in collectionsWithRelease)
                    {
                        var rank = (decimal)(collectionWithRelease.CollectionCount * .01 - (collectionWithRelease.ListNumber - 1) * .01);
                        releaseCollectionRank += rank;
                    }

                    release.Rank = SafeParser.ToNumber<decimal>(releaseTrackAverage) + releaseUserRatingRank + releaseCollectionRank;

                    await DbContext.SaveChangesAsync().ConfigureAwait(false);
                    CacheManager.ClearRegion(release.CacheRegion);
                    Logger.LogTrace("UpdateReleaseRank For Release `{0}`", release);
                    if (updateArtistRank)
                    {
                        await UpdateArtistsRankForRelease(release).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error UpdateReleaseRank RelaseId [{0}], UpdateArtistRank [{1}]", releaseId, updateArtistRank);
            }
        }
    }
}