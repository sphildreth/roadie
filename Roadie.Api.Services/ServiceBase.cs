using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Identity;
using Roadie.Library.Models;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
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

        protected readonly data.IRoadieDbContext _dbContext;

        protected readonly IHttpContext _httpContext;

        protected readonly IHttpEncoder _httpEncoder;

        protected readonly ILogger _logger;

        private static readonly Lazy<Hashids> hashIds = new Lazy<Hashids>(() =>
                                                        {
                                                            return new Hashids(TrackTokenSalt);
                                                        });

        protected ICacheManager CacheManager => _cacheManager;

        protected IRoadieSettings Configuration => _configuration;

        protected data.IRoadieDbContext DbContext => _dbContext;

        protected IHttpContext HttpContext => _httpContext;

        protected IHttpEncoder HttpEncoder => _httpEncoder;

        protected ILogger Logger => _logger;

        public ServiceBase(IRoadieSettings configuration, IHttpEncoder httpEncoder, data.IRoadieDbContext context,
                                                            ICacheManager cacheManager, ILogger logger, IHttpContext httpContext)
        {
            _configuration = configuration;
            _httpEncoder = httpEncoder;
            _dbContext = context;
            _cacheManager = cacheManager;
            _logger = logger;
            _httpContext = httpContext;
        }

        public static bool ConfirmTrackPlayToken(ApplicationUser user, Guid trackRoadieId, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            return TrackPlayToken(user, trackRoadieId).Equals(token);
        }

        public static string MakeTrackPlayUrl(ApplicationUser user, string baseUrl, int trackId, Guid trackRoadieId)
        {
            return $"{baseUrl}/play/track/{user.Id}/{TrackPlayToken(user, trackRoadieId)}/{trackRoadieId}.mp3";
        }

        public static string TrackPlayToken(ApplicationUser user, Guid trackId)
        {
            var trackIdPart = BitConverter.ToInt32(trackId.ToByteArray(), 6);
            if (trackIdPart < 0)
            {
                trackIdPart *= -1;
            }
            var token = hashIds.Value.Encode(user.Id, SafeParser.ToNumber<int>(user.CreatedDate.Value.ToString("DDHHmmss")), trackIdPart);
            return token;
        }

        public static Image MakeThumbnailImage(IRoadieSettings configuration, IHttpContext httpContext, Guid id, string type, int? width = null, int? height = null, bool includeCachebuster = false)
        {
            return MakeImage(configuration, httpContext, id, type, width ?? configuration.ThumbnailImageSize.Width, height ?? configuration.ThumbnailImageSize.Height, null, includeCachebuster);
        }

        protected IEnumerable<int> ArtistIdsForRelease(int releaseId)
        {
            var trackArtistIds = (from r in DbContext.Releases
                                  join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                  join tr in DbContext.Tracks on rm.Id equals tr.ReleaseMediaId
                                  where r.Id == releaseId
                                  where tr.ArtistId != null
                                  select tr.ArtistId.Value).ToList();
            trackArtistIds.Add(DbContext.Releases.FirstOrDefault(x => x.Id == releaseId).ArtistId);
            return trackArtistIds.Distinct().ToArray();
        }

        protected data.Artist GetArtist(string artistName)
        {
            if (string.IsNullOrEmpty(artistName)) return null;
            var artistByName = CacheManager.Get(data.Artist.CacheUrnByName(artistName), () =>
            {
                return DbContext.Artists
                    .FirstOrDefault(x => x.Name == artistName);
            }, null);
            if (artistByName == null) return null;
            return GetArtist(artistByName.RoadieId);
        }

        protected data.Artist GetArtist(Guid id)
        {
            return CacheManager.Get(data.Artist.CacheUrn(id), () =>
            {
                return DbContext.Artists
                    .Include(x => x.Genres)
                    .Include("Genres.Genre")
                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Artist.CacheRegionUrn(id));
        }

        protected data.Collection GetCollection(Guid id)
        {
            return CacheManager.Get(data.Collection.CacheUrn(id), () =>
            {
                return DbContext.Collections
                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Collection.CacheRegionUrn(id));
        }

        protected data.Genre GetGenre(Guid id)
        {
            return CacheManager.Get(data.Genre.CacheUrn(id), () =>
            {
                return DbContext.Genres
                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Genre.CacheRegionUrn(id));
        }

        protected data.Label GetLabel(Guid id)
        {
            return CacheManager.Get(data.Label.CacheUrn(id), () =>
            {
                return DbContext.Labels
                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Label.CacheRegionUrn(id));
        }

        protected data.Playlist GetPlaylist(Guid id)
        {
            return CacheManager.Get(data.Playlist.CacheUrn(id), () =>
            {
                return DbContext.Playlists
                    .Include(x => x.User)
                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Playlist.CacheRegionUrn(id));
        }

        protected data.Release GetRelease(Guid id)
        {
            return CacheManager.Get(data.Release.CacheUrn(id), () =>
            {
                return DbContext.Releases
                    .Include(x => x.Artist)
                    .Include(x => x.Genres)
                    .Include("Genres.Genre")
                    .Include(x => x.Medias)
                    .Include("Medias.Tracks")
                    .Include("Medias.Tracks.TrackArtist")
                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Release.CacheRegionUrn(id));
        }

        /// <summary>
        ///     Get Track by Subsonic Id ("T:guid")
        /// </summary>
        protected data.Track GetTrack(string id)
        {
            var trackId = Guid.Empty;
            if (Guid.TryParse(id, out trackId)) return GetTrack(trackId);
            return null;
        }

        // Only read operations
        protected data.Track GetTrack(Guid id)
        {
            return CacheManager.Get(data.Track.CacheUrn(id), () =>
            {
                return DbContext.Tracks
                    .Include(x => x.ReleaseMedia)
                    .Include(x => x.ReleaseMedia.Release)
                    .Include(x => x.ReleaseMedia.Release.Artist)
                    .Include(x => x.TrackArtist)
                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Track.CacheRegionUrn(id));
        }

        protected ApplicationUser GetUser(string username)
        {
            if (string.IsNullOrEmpty(username)) return null;
            var userByUsername = CacheManager.Get(ApplicationUser.CacheUrnByUsername(username),
                () => { return DbContext.Users.FirstOrDefault(x => x.UserName == username); }, null);
            return GetUser(userByUsername?.RoadieId);
        }

        protected ApplicationUser GetUser(Guid? id)
        {
            if (!id.HasValue) return null;
            return CacheManager.Get(ApplicationUser.CacheUrn(id.Value), () =>
            {
                return DbContext.Users
                    .Include(x => x.UserRoles)
                    .Include("UserRoles.Role")
                    .Include("UserRoles.Role.RoleClaims")
                    .Include(x => x.Claims)
                    .Include(x => x.UserQues)
                    .Include("UserQues.Track")
                    .FirstOrDefault(x => x.RoadieId == id);
            }, ApplicationUser.CacheRegionUrn(id.Value));
        }

        protected static Image MakeArtistThumbnailImage(IRoadieSettings configuration, IHttpContext httpContext, Guid? id)
        {
            if (!id.HasValue) return null;
            return MakeThumbnailImage(configuration, httpContext, id.Value, "artist");
        }

        protected static Image MakeCollectionThumbnailImage(IRoadieSettings configuration, IHttpContext httpContext, Guid id)
        {
            return MakeThumbnailImage(configuration, httpContext, id, "collection");
        }

        protected static Image MakeFullsizeImage(IRoadieSettings configuration, IHttpContext httpContext, Guid id, string caption = null)
        {
            return new Image($"{httpContext.ImageBaseUrl}/{id}", caption,
                $"{httpContext.ImageBaseUrl}/{id}/{configuration.SmallImageSize.Width}/{configuration.SmallImageSize.Height}");
        }

        protected static Image MakeFullsizeSecondaryImage(IRoadieSettings configuration, IHttpContext httpContext, Guid id, ImageType type, int imageId, string caption = null)
        {
            if (type == ImageType.ArtistSecondary)
            {
                return new Image($"{httpContext.ImageBaseUrl}/artist-secondary/{id}/{imageId}", caption,
                    $"{httpContext.ImageBaseUrl}/artist-secondary/{id}/{imageId}/{configuration.SmallImageSize.Width}/{configuration.SmallImageSize.Height}");
            }
            return new Image($"{httpContext.ImageBaseUrl}/release-secondary/{id}/{imageId}", caption,
                $"{httpContext.ImageBaseUrl}/release-secondary/{id}/{imageId}/{configuration.SmallImageSize.Width}/{configuration.SmallImageSize.Height}");
        }

        protected static Image MakeGenreThumbnailImage(IRoadieSettings configuration, IHttpContext httpContext, Guid id)
        {
            return MakeThumbnailImage(configuration, httpContext, id, "genre");
        }

        protected static Image MakeImage(IRoadieSettings configuration, IHttpContext httpContext, Guid id, int width = 200, int height = 200, string caption = null, bool includeCachebuster = false)
        {
            return new Image(
                $"{httpContext.ImageBaseUrl}/{id}/{width}/{height}/{(includeCachebuster ? DateTime.UtcNow.Ticks.ToString() : string.Empty)}",
                caption,
                $"{httpContext.ImageBaseUrl}/{id}/{configuration.SmallImageSize.Width}/{configuration.SmallImageSize.Height}");
        }

        protected static Image MakeImage(IRoadieSettings configuration, IHttpContext httpContext, Guid id, string type, IImageSize imageSize)
        {
            return MakeImage(configuration, httpContext, id, type, imageSize.Width, imageSize.Height);
        }
        private static Image MakeImage(IRoadieSettings configuration, IHttpContext httpContext, Guid id, string type, int? width, int? height, string caption = null, bool includeCachebuster = false)
        {
            if (width.HasValue && height.HasValue && (width.Value != configuration.ThumbnailImageSize.Width ||
                                                      height.Value != configuration.ThumbnailImageSize.Height))
                return new Image(
                    $"{httpContext.ImageBaseUrl}/{type}/{id}/{width}/{height}/{(includeCachebuster ? DateTime.UtcNow.Ticks.ToString() : string.Empty)}",
                    caption,
                    $"{httpContext.ImageBaseUrl}/{type}/{id}/{configuration.ThumbnailImageSize.Width}/{configuration.ThumbnailImageSize.Height}");
            return new Image($"{httpContext.ImageBaseUrl}/{type}/{id}", caption, null);
        }

        protected static Image MakeLabelThumbnailImage(IRoadieSettings configuration, IHttpContext httpContext, Guid id)
        {
            return MakeThumbnailImage(configuration, httpContext, id, "label");
        }

        protected string MakeLastFmUrl(string artistName, string releaseTitle)
        {
            return "http://www.last.fm/music/" + HttpEncoder.UrlEncode($"{artistName}/{releaseTitle}");
        }

        protected Image MakeNewImage(string type)
        {
            return new Image($"{HttpContext.ImageBaseUrl}/{type}.jpg", null, null);
        }

        protected static Image MakePlaylistThumbnailImage(IRoadieSettings configuration, IHttpContext httpContext, Guid id)
        {
            return MakeThumbnailImage(configuration, httpContext, id, "playlist");
        }

        protected static Image MakeReleaseThumbnailImage(IRoadieSettings configuration, IHttpContext httpContext, Guid id)
        {
            return MakeThumbnailImage(configuration, httpContext, id, "release");
        }

        protected static Image MakeTrackThumbnailImage(IRoadieSettings configuration, IHttpContext httpContext, Guid id)
        {
            return MakeThumbnailImage(configuration, httpContext, id, "track");
        }

        protected static Image MakeUserThumbnailImage(IRoadieSettings configuration, IHttpContext httpContext, Guid id)
        {
            return MakeThumbnailImage(configuration, httpContext, id, "user");
        }

        protected async Task<OperationResult<short>> SetArtistRating(Guid artistId, ApplicationUser user, short rating)
        {
            var artist = DbContext.Artists
                .Include(x => x.Genres)
                .Include("Genres.Genre")
                .FirstOrDefault(x => x.RoadieId == artistId);
            if (artist == null) return new OperationResult<short>(true, $"Invalid Artist Id [{artistId}]");
            var now = DateTime.UtcNow;
            var userArtist = DbContext.UserArtists.FirstOrDefault(x => x.ArtistId == artist.Id && x.UserId == user.Id);
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

            await DbContext.SaveChangesAsync();

            var ratings = DbContext.UserArtists.Where(x => x.ArtistId == artist.Id && x.Rating > 0)
                .Select(x => x.Rating);
            if (ratings != null && ratings.Any())
                artist.Rating = (short)ratings.Average(x => (decimal)x);
            else
                artist.Rating = 0;
            artist.LastUpdated = now;
            await DbContext.SaveChangesAsync();
            await UpdateArtistRank(artist.Id);
            CacheManager.ClearRegion(user.CacheRegion);
            CacheManager.ClearRegion(artist.CacheRegion);

            artist = GetArtist(artistId);

            return new OperationResult<short>
            {
                IsSuccess = true,
                Data = artist.Rating ?? 0
            };
        }

        protected async Task<OperationResult<short>> SetReleaseRating(Guid releaseId, ApplicationUser user,
            short rating)
        {
            var release = DbContext.Releases
                .Include(x => x.Artist)
                .Include(x => x.Genres)
                .Include("Genres.Genre")
                .Include(x => x.Medias)
                .Include("Medias.Tracks")
                .Include("Medias.Tracks.TrackArtist")
                .FirstOrDefault(x => x.RoadieId == releaseId);
            if (release == null) return new OperationResult<short>(true, $"Invalid Release Id [{releaseId}]");
            var userRelease =
                DbContext.UserReleases.FirstOrDefault(x => x.ReleaseId == release.Id && x.UserId == user.Id);
            var now = DateTime.UtcNow;
            if (userRelease == null)
            {
                userRelease = new data.UserRelease
                {
                    Rating = rating,
                    UserId = user.Id,
                    ReleaseId = release.Id
                };
                DbContext.UserReleases.Add(userRelease);
            }
            else
            {
                userRelease.Rating = rating;
                userRelease.LastUpdated = now;
            }

            await DbContext.SaveChangesAsync();

            var ratings = DbContext.UserReleases.Where(x => x.ReleaseId == release.Id && x.Rating > 0)
                .Select(x => x.Rating);
            if (ratings != null && ratings.Any())
                release.Rating = (short)ratings.Average(x => (decimal)x);
            else
                release.Rating = 0;
            release.LastUpdated = now;
            await DbContext.SaveChangesAsync();
            await UpdateReleaseRank(release.Id);
            CacheManager.ClearRegion(user.CacheRegion);
            CacheManager.ClearRegion(release.CacheRegion);
            CacheManager.ClearRegion(release.Artist.CacheRegion);

            release = GetRelease(releaseId);

            return new OperationResult<short>
            {
                IsSuccess = true,
                Data = release.Rating ?? 0
            };
        }

        protected async Task<OperationResult<short>> SetTrackRating(Guid trackId, ApplicationUser user, short rating)
        {
            var sw = Stopwatch.StartNew();

            var track = DbContext.Tracks
                .Include(x => x.ReleaseMedia)
                .Include(x => x.ReleaseMedia.Release)
                .Include(x => x.ReleaseMedia.Release.Artist)
                .Include(x => x.TrackArtist)
                .FirstOrDefault(x => x.RoadieId == trackId);
            if (track == null) return new OperationResult<short>(true, $"Invalid Track Id [{trackId}]");
            var now = DateTime.UtcNow;
            var userTrack = DbContext.UserTracks.FirstOrDefault(x => x.TrackId == track.Id && x.UserId == user.Id);
            if (userTrack == null)
            {
                userTrack = new data.UserTrack
                {
                    Rating = rating,
                    UserId = user.Id,
                    TrackId = track.Id
                };
                DbContext.UserTracks.Add(userTrack);
            }
            else
            {
                userTrack.Rating = rating;
                userTrack.LastUpdated = now;
            }

            await DbContext.SaveChangesAsync();

            var ratings = DbContext.UserTracks.Where(x => x.TrackId == track.Id && x.Rating > 0).Select(x => x.Rating);
            if (ratings != null && ratings.Any())
                track.Rating = (short)ratings.Average(x => (decimal)x);
            else
                track.Rating = 0;
            track.LastUpdated = now;
            await DbContext.SaveChangesAsync();
            await UpdateReleaseRank(track.ReleaseMedia.Release.Id);

            CacheManager.ClearRegion(user.CacheRegion);
            CacheManager.ClearRegion(track.CacheRegion);
            CacheManager.ClearRegion(track.ReleaseMedia.Release.CacheRegion);
            CacheManager.ClearRegion(track.ReleaseMedia.Release.Artist.CacheRegion);
            if (track.TrackArtist != null) CacheManager.ClearRegion(track.TrackArtist.CacheRegion);

            sw.Stop();
            return new OperationResult<short>
            {
                IsSuccess = true,
                Data = track.Rating,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        protected async Task<OperationResult<bool>> ToggleArtistDisliked(Guid artistId, ApplicationUser user,
            bool isDisliked)
        {
            var artist = DbContext.Artists
                .Include(x => x.Genres)
                .Include("Genres.Genre")
                .FirstOrDefault(x => x.RoadieId == artistId);
            if (artist == null) return new OperationResult<bool>(true, $"Invalid Artist Id [{artistId}]");
            var userArtist = DbContext.UserArtists.FirstOrDefault(x => x.ArtistId == artist.Id && x.UserId == user.Id);
            if (userArtist == null)
            {
                userArtist = new data.UserArtist
                {
                    IsDisliked = isDisliked,
                    UserId = user.Id,
                    ArtistId = artist.Id
                };
                DbContext.UserArtists.Add(userArtist);
            }
            else
            {
                userArtist.IsDisliked = isDisliked;
                userArtist.LastUpdated = DateTime.UtcNow;
            }

            await DbContext.SaveChangesAsync();

            CacheManager.ClearRegion(user.CacheRegion);
            CacheManager.ClearRegion(artist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        protected async Task<OperationResult<bool>> ToggleArtistFavorite(Guid artistId, ApplicationUser user, bool isFavorite)
        {
            var artist = DbContext.Artists
                .Include(x => x.Genres)
                .Include("Genres.Genre")
                .FirstOrDefault(x => x.RoadieId == artistId);
            if (artist == null) return new OperationResult<bool>(true, $"Invalid Artist Id [{artistId}]");
            var userArtist = DbContext.UserArtists.FirstOrDefault(x => x.ArtistId == artist.Id && x.UserId == user.Id);
            if (userArtist == null)
            {
                userArtist = new data.UserArtist
                {
                    IsFavorite = isFavorite,
                    UserId = user.Id,
                    ArtistId = artist.Id
                };
                DbContext.UserArtists.Add(userArtist);
            }
            else
            {
                userArtist.IsFavorite = isFavorite;
                userArtist.LastUpdated = DateTime.UtcNow;
            }

            await DbContext.SaveChangesAsync();

            CacheManager.ClearRegion(user.CacheRegion);
            CacheManager.ClearRegion(artist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        protected async Task<OperationResult<bool>> ToggleReleaseDisliked(Guid releaseId, ApplicationUser user, bool isDisliked)
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
            var userRelease = DbContext.UserReleases.FirstOrDefault(x => x.ReleaseId == release.Id && x.UserId == user.Id);
            if (userRelease == null)
            {
                userRelease = new data.UserRelease
                {
                    IsDisliked = isDisliked,
                    UserId = user.Id,
                    ReleaseId = release.Id
                };
                DbContext.UserReleases.Add(userRelease);
            }
            else
            {
                userRelease.IsDisliked = isDisliked;
                userRelease.LastUpdated = DateTime.UtcNow;
            }

            await DbContext.SaveChangesAsync();

            CacheManager.ClearRegion(user.CacheRegion);
            CacheManager.ClearRegion(release.CacheRegion);
            CacheManager.ClearRegion(release.Artist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        protected async Task<OperationResult<bool>> ToggleReleaseFavorite(Guid releaseId, ApplicationUser user,
            bool isFavorite)
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
            var userRelease = DbContext.UserReleases.FirstOrDefault(x => x.ReleaseId == release.Id && x.UserId == user.Id);
            if (userRelease == null)
            {
                userRelease = new data.UserRelease
                {
                    IsFavorite = isFavorite,
                    UserId = user.Id,
                    ReleaseId = release.Id
                };
                DbContext.UserReleases.Add(userRelease);
            }
            else
            {
                userRelease.IsFavorite = isFavorite;
                userRelease.LastUpdated = DateTime.UtcNow;
            }

            await DbContext.SaveChangesAsync();

            CacheManager.ClearRegion(user.CacheRegion);
            CacheManager.ClearRegion(release.CacheRegion);
            CacheManager.ClearRegion(release.Artist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        protected async Task<OperationResult<bool>> ToggleTrackDisliked(Guid trackId, ApplicationUser user, bool isDisliked)
        {
            var track = DbContext.Tracks
                .Include(x => x.ReleaseMedia)
                .Include(x => x.ReleaseMedia.Release)
                .Include(x => x.ReleaseMedia.Release.Artist)
                .Include(x => x.TrackArtist)
                .FirstOrDefault(x => x.RoadieId == trackId);
            if (track == null)
            {
                return new OperationResult<bool>(true, $"Invalid Track Id [{trackId}]");
            }
            var userTrack = DbContext.UserTracks.FirstOrDefault(x => x.TrackId == track.Id && x.UserId == user.Id);
            if (userTrack == null)
            {
                userTrack = new data.UserTrack
                {
                    IsDisliked = isDisliked,
                    UserId = user.Id,
                    TrackId = track.Id
                };
                DbContext.UserTracks.Add(userTrack);
            }
            else
            {
                userTrack.IsDisliked = isDisliked;
                userTrack.LastUpdated = DateTime.UtcNow;
            }

            await DbContext.SaveChangesAsync();

            CacheManager.ClearRegion(user.CacheRegion);
            CacheManager.ClearRegion(track.CacheRegion);
            CacheManager.ClearRegion(track.ReleaseMedia.Release.CacheRegion);
            CacheManager.ClearRegion(track.ReleaseMedia.Release.Artist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        protected async Task<OperationResult<bool>> ToggleTrackFavorite(Guid trackId, ApplicationUser user, bool isFavorite)
        {
            var track = DbContext.Tracks
                .Include(x => x.ReleaseMedia)
                .Include(x => x.ReleaseMedia.Release)
                .Include(x => x.ReleaseMedia.Release.Artist)
                .Include(x => x.TrackArtist)
                .FirstOrDefault(x => x.RoadieId == trackId);
            if (track == null)
            {
                return new OperationResult<bool>(true, $"Invalid Track Id [{trackId}]");
            }
            var userTrack = DbContext.UserTracks.FirstOrDefault(x => x.TrackId == track.Id && x.UserId == user.Id);
            if (userTrack == null)
            {
                userTrack = new data.UserTrack
                {
                    IsFavorite = isFavorite,
                    UserId = user.Id,
                    TrackId = track.Id
                };
                DbContext.UserTracks.Add(userTrack);
            }
            else
            {
                userTrack.IsFavorite = isFavorite;
                userTrack.LastUpdated = DateTime.UtcNow;
            }

            await DbContext.SaveChangesAsync();

            CacheManager.ClearRegion(user.CacheRegion);
            CacheManager.ClearRegion(track.CacheRegion);
            CacheManager.ClearRegion(track.ReleaseMedia.Release.CacheRegion);
            CacheManager.ClearRegion(track.ReleaseMedia.Release.Artist.CacheRegion);

            Logger.LogDebug($"ToggleTrackFavorite: User `{ user }`, Track `{ track }`, isFavorite: [{ isFavorite }]");

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        protected async Task UpdateArtistCounts(int artistId, DateTime now)
        {
            var artist = DbContext.Artists.FirstOrDefault(x => x.Id == artistId);
            if (artist != null)
            {
                artist.ReleaseCount = DbContext.Releases.Where(x => x.ArtistId == artistId).Count();
                artist.TrackCount = (from r in DbContext.Releases
                                     join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                     join tr in DbContext.Tracks on rm.Id equals tr.ReleaseMediaId
                                     where tr.ArtistId == artistId || r.ArtistId == artistId
                                     select tr).Count();

                artist.LastUpdated = now;
                await DbContext.SaveChangesAsync();
                CacheManager.ClearRegion(artist.CacheRegion);
            }
        }

        /// <summary>
        ///     Update the counts for all artists on a release (both track and release artists)
        /// </summary>
        protected async Task UpdateArtistCountsForRelease(int releaseId, DateTime now)
        {
            foreach (var artistId in ArtistIdsForRelease(releaseId))
            {
                await UpdateArtistCounts(artistId, now);
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
                var artist = DbContext.Artists.FirstOrDefault(x => x.Id == artistId);
                if (artist != null)
                {
                    if (updateReleaseRanks)
                    {
                        var artistReleaseIds = DbContext.Releases.Where(x => x.ArtistId == artistId).Select(x => x.Id)
                            .ToArray();
                        foreach (var artistReleaseId in artistReleaseIds)
                            await UpdateReleaseRank(artistReleaseId, false);
                    }

                    var artistTrackAverage = (from t in DbContext.Tracks
                                              join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                              join ut in DbContext.UserTracks on t.Id equals ut.TrackId
                                              where t.ArtistId == artist.Id
                                              select ut.Rating).Select(x => (decimal?)x).Average();

                    var artistReleaseRatingRating = (from r in DbContext.Releases
                                                     join ur in DbContext.UserReleases on r.Id equals ur.ReleaseId
                                                     where r.ArtistId == artist.Id
                                                     select ur.Rating).Select(x => (decimal?)x).Average();

                    var artistReleaseRankSum = (from r in DbContext.Releases
                                                where r.ArtistId == artist.Id
                                                select r.Rank).ToArray().Sum(x => x) ?? 0;

                    artist.Rank = SafeParser.ToNumber<decimal>(artistTrackAverage + artistReleaseRatingRating) +
                                  artistReleaseRankSum + artist.Rating;

                    await DbContext.SaveChangesAsync();
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
                var trackArtistsForRelease = (from t in DbContext.Tracks
                                              join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                              where rm.ReleaseId == release.Id
                                              where t.ArtistId.HasValue
                                              select t.ArtistId.Value).ToArray();
                artistsForRelease.AddRange(trackArtistsForRelease);
                foreach (var artistId in artistsForRelease.Distinct()) await UpdateArtistRank(artistId);
            }
        }

        protected async Task UpdateLabelCounts(int labelId, DateTime now)
        {
            var label = DbContext.Labels.FirstOrDefault(x => x.Id == labelId);
            if (label != null)
            {
                label.ReleaseCount = DbContext.ReleaseLabels.Where(x => x.LabelId == label.Id).Count();
                label.ArtistCount = (from r in DbContext.Releases
                                     join rl in DbContext.ReleaseLabels on r.Id equals rl.ReleaseId
                                     join a in DbContext.Artists on r.ArtistId equals a.Id
                                     where rl.LabelId == label.Id
                                     group a by a.Id
                    into artists
                                     select artists).Select(x => x.Key).Count();
                label.TrackCount = (from r in DbContext.Releases
                                    join rl in DbContext.ReleaseLabels on r.Id equals rl.ReleaseId
                                    join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                    join t in DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                    where rl.LabelId == label.Id
                                    select t).Count();
                await DbContext.SaveChangesAsync();
                CacheManager.ClearRegion(label.CacheRegion);
            }
        }

        protected async Task UpdatePlaylistCounts(int playlistId, DateTime now)
        {
            var playlist = DbContext.Playlists.FirstOrDefault(x => x.Id == playlistId);
            if (playlist != null)
            {
                var playlistTracks = DbContext.PlaylistTracks
                    .Include(x => x.Track)
                    .Include("Track.ReleaseMedia")
                    .Where(x => x.PlayListId == playlist.Id).ToArray();
                playlist.TrackCount = (short)playlistTracks.Count();
                playlist.Duration = playlistTracks.Sum(x => x.Track.Duration);
                playlist.ReleaseCount =
                    (short)playlistTracks.Select(x => x.Track.ReleaseMedia.ReleaseId).Distinct().Count();
                playlist.LastUpdated = now;
                await DbContext.SaveChangesAsync();
                CacheManager.ClearRegion(playlist.CacheRegion);
            }
        }

        protected async Task UpdateReleaseCounts(int releaseId, DateTime now)
        {
            var release = DbContext.Releases.FirstOrDefault(x => x.Id == releaseId);
            if (release != null)
            {
                release.PlayedCount = (from t in DbContext.Tracks
                                       join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                       where rm.ReleaseId == releaseId
                                       where t.PlayedCount.HasValue
                                       select t).Sum(x => x.PlayedCount);
                release.Duration = (from t in DbContext.Tracks
                                    join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                    where rm.ReleaseId == releaseId
                                    select t).Sum(x => x.Duration);
                await DbContext.SaveChangesAsync();
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
                var release = DbContext.Releases.FirstOrDefault(x => x.Id == releaseId);
                if (release != null)
                {
                    var releaseTrackAverage = (from ut in DbContext.UserTracks
                                               join t in DbContext.Tracks on ut.TrackId equals t.Id
                                               join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                               where rm.ReleaseId == releaseId
                                               select ut.Rating).Select(x => (decimal?)x).Average();

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
                        var rank = (decimal)(collectionWithRelease.CollectionCount * .01 -
                                              (collectionWithRelease.ListNumber - 1) * .01);
                        releaseCollectionRank += rank;
                    }

                    release.Rank = SafeParser.ToNumber<decimal>(releaseTrackAverage) + releaseUserRatingRank +
                                   releaseCollectionRank;

                    await DbContext.SaveChangesAsync();
                    CacheManager.ClearRegion(release.CacheRegion);
                    Logger.LogTrace("UpdateReleaseRank For Release `{0}`", release);
                    if (updateArtistRank) await UpdateArtistsRankForRelease(release);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error UpdateReleaseRank RelaseId [{0}], UpdateArtistRank [{1}]", releaseId,
                    updateArtistRank);
            }
        }


    }
}