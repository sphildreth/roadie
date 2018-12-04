using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Identity;
using Roadie.Library.Models;
using Roadie.Library.Utility;
using System;
using System.Linq;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public abstract class ServiceBase
    {
        protected readonly ICacheManager _cacheManager = null;
        protected readonly IRoadieSettings _configuration = null;
        protected readonly data.IRoadieDbContext _dbContext = null;
        protected readonly IHttpContext _httpContext = null;
        protected readonly IHttpEncoder _httpEncoder = null;
        protected readonly ILogger _logger = null;

        protected ICacheManager CacheManager
        {
            get
            {
                return this._cacheManager;
            }
        }

        protected IRoadieSettings Configuration
        {
            get
            {
                return this._configuration;
            }
        }

        protected data.IRoadieDbContext DbContext
        {
            get
            {
                return this._dbContext;
            }
        }

        protected IHttpContext HttpContext
        {
            get
            {
                return this._httpContext;
            }
        }

        protected IHttpEncoder HttpEncoder
        {
            get
            {
                return this._httpEncoder;
            }
        }

        protected ILogger Logger
        {
            get
            {
                return this._logger;
            }
        }

        public ServiceBase(IRoadieSettings configuration, IHttpEncoder httpEncoder, data.IRoadieDbContext context,
                             ICacheManager cacheManager, ILogger logger, IHttpContext httpContext)
        {
            this._configuration = configuration;
            this._httpEncoder = httpEncoder;
            this._dbContext = context;
            this._cacheManager = cacheManager;
            this._logger = logger;
            this._httpContext = httpContext;
        }

        public Image MakeThumbnailImage(Guid id, string type, int? width = null, int? height = null)
        {
            return this.MakeImage(id, type, width ?? this.Configuration.ThumbnailImageSize.Width, height ?? this.Configuration.ThumbnailImageSize.Height);
        }

        protected data.Artist GetArtist(string artistName)
        {
            if (string.IsNullOrEmpty(artistName))
            {
                return null;
            }
            var artistByName = this.CacheManager.Get(data.Artist.CacheUrnByName(artistName), () =>
            {
                return this.DbContext.Artists
                                    .FirstOrDefault(x => x.Name == artistName);
            }, null);
            if (artistByName == null)
            {
                return null;
            }
            return this.GetArtist(artistByName.RoadieId);
        }

        protected data.Artist GetArtist(Guid id)
        {
            return this.CacheManager.Get(data.Artist.CacheUrn(id), () =>
            {
                return this.DbContext.Artists
                                    .Include(x => x.Genres)
                                    .Include("Genres.Genre")
                                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Artist.CacheRegionUrn(id));
        }

        protected data.Collection GetCollection(Guid id)
        {
            return this.CacheManager.Get(data.Collection.CacheUrn(id), () =>
            {
                return this.DbContext.Collections
                                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Collection.CacheRegionUrn(id));
        }

        protected data.Label GetLabel(Guid id)
        {
            return this.CacheManager.Get(data.Label.CacheUrn(id), () =>
            {
                return this.DbContext.Labels
                                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Label.CacheRegionUrn(id));
        }

        protected data.Playlist GetPlaylist(Guid id)
        {
            return this.CacheManager.Get(data.Playlist.CacheUrn(id), () =>
            {
                return this.DbContext.Playlists
                                    .Include(x => x.User)
                                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Playlist.CacheRegionUrn(id));
        }

        protected data.Release GetRelease(Guid id)
        {
            return this.CacheManager.Get(data.Release.CacheUrn(id), () =>
            {
                return this.DbContext.Releases
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
        /// Get Track by Subsonic Id ("T:guid")
        /// </summary>
        protected data.Track GetTrack(string id)
        {
            Guid trackId = Guid.Empty;
            if (Guid.TryParse(id, out trackId))
            {
                return this.GetTrack(trackId);
            }
            return null;
        }

        protected data.Track GetTrack(Guid id)
        {
            return this.CacheManager.Get(data.Track.CacheUrn(id), () =>
            {
                return this.DbContext.Tracks
                                     .Include(x => x.ReleaseMedia)
                                     .Include(x => x.ReleaseMedia.Release)
                                     .Include(x => x.ReleaseMedia.Release.Artist)
                                     .Include(x => x.TrackArtist)
                                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Track.CacheRegionUrn(id));
        }

        protected ApplicationUser GetUser(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }
            var userByUsername = this.CacheManager.Get(ApplicationUser.CacheUrnByUsername(username), () =>
            {
                return this.DbContext.Users
                                    .FirstOrDefault(x => x.UserName == username);
            }, null);
            return this.GetUser(userByUsername?.RoadieId);
        }

        protected ApplicationUser GetUser(Guid? id)
        {
            if (!id.HasValue)
            {
                return null;
            }
            return this.CacheManager.Get(ApplicationUser.CacheUrn(id.Value), () =>
            {
                return this.DbContext.Users
                                    .Include(x => x.ArtistRatings)
                                    .Include(x => x.ReleaseRatings)
                                    .Include(x => x.TrackRatings)
                                    .Include(x => x.UserRoles)
                                    .Include("UserRoles.Role")
                                    .Include("UserRoles.Role.RoleClaims")
                                    .Include(x => x.Claims)
                                    .Include(x => x.UserQues)
                                    .Include("UserQues.Track")
                                    .FirstOrDefault(x => x.RoadieId == id);
            }, ApplicationUser.CacheRegionUrn(id.Value));
        }

        protected Image MakeArtistThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "artist");
        }

        protected Image MakeCollectionThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "collection");
        }

        protected Image MakeImage(Guid id, int width = 200, int height = 200, string caption = null)
        {
            return new Image($"{this.HttpContext.ImageBaseUrl }/{id}/{ width }/{ height }", caption, $"{this.HttpContext.ImageBaseUrl }/{id}/{ this.Configuration.SmallImageSize.Width }/{ this.Configuration.SmallImageSize.Height }");
        }

        protected Image MakeFullsizeImage(Guid id, string caption = null)
        {
            return new Image($"{this.HttpContext.ImageBaseUrl }/{id}", caption,  $"{this.HttpContext.ImageBaseUrl }/{id}/{ this.Configuration.SmallImageSize.Width }/{ this.Configuration.SmallImageSize.Height }");
        }

        protected Image MakeImage(Guid id, string type, ImageSize imageSize)
        {
            return this.MakeImage(id, type, imageSize.Width, imageSize.Height);
        }

        protected Image MakeLabelThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "label");
        }

        protected string MakeLastFmUrl(string artistName, string releaseTitle)
        {
            return "http://www.last.fm/music/" + this.HttpEncoder.UrlEncode($"{ artistName }/{ releaseTitle }");
        }

        protected Image MakePlaylistThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "playlist");
        }

        protected Image MakeReleaseThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "release");
        }

        protected Image MakeTrackThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "track");
        }

        protected Image MakeUserThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "user");
        }

        protected async Task<OperationResult<short>> SetArtistRating(Guid artistId, ApplicationUser user, short rating)
        {
            var artist = this.GetArtist(artistId);
            if (artist == null)
            {
                return new OperationResult<short>(true, $"Invalid Artist Id [{ artistId }]");
            }
            var userArtist = this.DbContext.UserArtists.FirstOrDefault(x => x.ArtistId == artist.Id && x.UserId == user.Id);
            if (userArtist == null)
            {
                userArtist = new data.UserArtist
                {
                    Rating = rating,
                    UserId = user.Id,
                    ArtistId = artist.Id
                };
                this.DbContext.UserArtists.Add(userArtist);
            }
            else
            {
                userArtist.Rating = rating;
                userArtist.LastUpdated = DateTime.UtcNow;
            }
            await this.DbContext.SaveChangesAsync();

            var sql = "UPDATE `artist` set lastUpdated = UTC_DATE(), rating = (SELECT cast(avg(ur.rating) as signed) " +
                      "FROM `userartist` ur " +
                      "where artistId = {0}) " +
                      "WHERE id = {0};";
            await this.DbContext.Database.ExecuteSqlCommandAsync(sql, artist.Id);

            this.CacheManager.ClearRegion(user.CacheRegion);
            this.CacheManager.ClearRegion(artist.CacheRegion);

            artist = this.GetArtist(artistId);

            return new OperationResult<short>
            {
                IsSuccess = true,
                Data = artist.Rating ?? 0
            };
        }

        protected async Task<OperationResult<short>> SetReleaseRating(Guid releaseId, ApplicationUser user, short rating)
        {
            var release = this.GetRelease(releaseId);
            if (release == null)
            {
                return new OperationResult<short>(true, $"Invalid Release Id [{ releaseId }]");
            }
            var userRelease = this.DbContext.UserReleases.FirstOrDefault(x => x.ReleaseId == release.Id && x.UserId == user.Id);
            var now = DateTime.UtcNow;
            if (userRelease == null)
            {
                userRelease = new data.UserRelease
                {
                    Rating = rating,
                    UserId = user.Id,
                    ReleaseId = release.Id
                };
                this.DbContext.UserReleases.Add(userRelease);
            }
            else
            {
                userRelease.Rating = rating;
                userRelease.LastUpdated = now;
            }
            await this.DbContext.SaveChangesAsync();

            var sql = "UPDATE `release` set lastUpdated = UTC_DATE(), rating = (SELECT cast(avg(ur.rating) as signed) " +
                      "FROM `userrelease` ur " +
                      "where releaseId = {0}) " +
                      "WHERE id = {0};";
            await this.DbContext.Database.ExecuteSqlCommandAsync(sql, release.Id);

            this.CacheManager.ClearRegion(user.CacheRegion);
            this.CacheManager.ClearRegion(release.CacheRegion);
            this.CacheManager.ClearRegion(release.Artist.CacheRegion);

            release = this.GetRelease(releaseId);

            return new OperationResult<short>
            {
                IsSuccess = true,
                Data = release.Rating ?? 0
            };
        }

        protected async Task<OperationResult<short>> SetTrackRating(Guid trackId, ApplicationUser user, short rating)
        {
            var track = this.GetTrack(trackId);
            if (track == null)
            {
                return new OperationResult<short>(true, $"Invalid Track Id [{ trackId }]");
            }
            var userTrack = this.DbContext.UserTracks.FirstOrDefault(x => x.TrackId == track.Id && x.UserId == user.Id);
            if (userTrack == null)
            {
                userTrack = new data.UserTrack
                {
                    Rating = rating,
                    UserId = user.Id,
                    TrackId = track.Id
                };
                this.DbContext.UserTracks.Add(userTrack);
            }
            else
            {
                userTrack.Rating = rating;
                userTrack.LastUpdated = DateTime.UtcNow;
            }
            await this.DbContext.SaveChangesAsync();

            var sql = "UPDATE `track` set lastUpdated = UTC_DATE(), rating = (SELECT cast(avg(ur.rating) as signed) " +
                      "FROM `usertrack` ur " +
                      "where trackId = {0}) " +
                      "WHERE id = {0};";
            await this.DbContext.Database.ExecuteSqlCommandAsync(sql, track.Id);

            this.CacheManager.ClearRegion(user.CacheRegion);
            this.CacheManager.ClearRegion(track.CacheRegion);
            this.CacheManager.ClearRegion(track.ReleaseMedia.Release.CacheRegion);
            this.CacheManager.ClearRegion(track.ReleaseMedia.Release.Artist.CacheRegion);

            track = this.GetTrack(trackId);

            return new OperationResult<short>
            {
                IsSuccess = true,
                Data = track.Rating
            };
        }

        protected async Task<OperationResult<bool>> ToggleArtistFavorite(Guid artistId, ApplicationUser user, bool isFavorite)
        {
            var artist = this.GetArtist(artistId);
            if (artist == null)
            {
                return new OperationResult<bool>(true, $"Invalid Artist Id [{ artistId }]");
            }
            var userArtist = this.DbContext.UserArtists.FirstOrDefault(x => x.ArtistId == artist.Id && x.UserId == user.Id);
            if (userArtist == null)
            {
                userArtist = new data.UserArtist
                {
                    IsFavorite = true,
                    UserId = user.Id,
                    ArtistId = artist.Id
                };
                this.DbContext.UserArtists.Add(userArtist);
            }
            else
            {
                userArtist.IsFavorite = isFavorite;
                userArtist.LastUpdated = DateTime.UtcNow;
            }
            await this.DbContext.SaveChangesAsync();

            this.CacheManager.ClearRegion(user.CacheRegion);
            this.CacheManager.ClearRegion(artist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        protected async Task<OperationResult<bool>> ToggleReleaseFavorite(Guid releaseId, ApplicationUser user, bool isFavorite)
        {
            var release = this.GetRelease(releaseId);
            if (release == null)
            {
                return new OperationResult<bool>(true, $"Invalid Release Id [{ releaseId }]");
            }
            var userRelease = this.DbContext.UserReleases.FirstOrDefault(x => x.ReleaseId == release.Id && x.UserId == user.Id);
            if (userRelease == null)
            {
                userRelease = new data.UserRelease
                {
                    IsFavorite = true,
                    UserId = user.Id,
                    ReleaseId = release.Id
                };
                this.DbContext.UserReleases.Add(userRelease);
            }
            else
            {
                userRelease.IsFavorite = isFavorite;
                userRelease.LastUpdated = DateTime.UtcNow;
            }
            await this.DbContext.SaveChangesAsync();

            this.CacheManager.ClearRegion(user.CacheRegion);
            this.CacheManager.ClearRegion(release.CacheRegion);
            this.CacheManager.ClearRegion(release.Artist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        protected async Task<OperationResult<bool>> ToggleTrackFavorite(Guid trackId, ApplicationUser user, bool isFavorite)
        {
            var track = this.GetTrack(trackId);
            if (track == null)
            {
                return new OperationResult<bool>(true, $"Invalid Track Id [{ trackId }]");
            }
            var userTrack = this.DbContext.UserTracks.FirstOrDefault(x => x.TrackId == track.Id && x.UserId == user.Id);
            if (userTrack == null)
            {
                userTrack = new data.UserTrack
                {
                    IsFavorite = true,
                    UserId = user.Id,
                    TrackId = track.Id
                };
                this.DbContext.UserTracks.Add(userTrack);
            }
            else
            {
                userTrack.IsFavorite = isFavorite;
                userTrack.LastUpdated = DateTime.UtcNow;
            }
            await this.DbContext.SaveChangesAsync();

            this.CacheManager.ClearRegion(user.CacheRegion);
            this.CacheManager.ClearRegion(track.CacheRegion);
            this.CacheManager.ClearRegion(track.ReleaseMedia.Release.CacheRegion);
            this.CacheManager.ClearRegion(track.ReleaseMedia.Release.Artist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        private Image MakeImage(Guid id, string type, int? width, int? height, string caption = null)
        {
            if (width.HasValue && height.HasValue && (width.Value != this.Configuration.ThumbnailImageSize.Width || height.Value != this.Configuration.ThumbnailImageSize.Height))
            {
                return new Image($"{this.HttpContext.ImageBaseUrl }/{type}/{id}/{width}/{height}", caption, $"{this.HttpContext.ImageBaseUrl }/{type}/{id}/{ this.Configuration.ThumbnailImageSize.Width }/{ this.Configuration.ThumbnailImageSize.Height }");
            }
            return new Image($"{this.HttpContext.ImageBaseUrl }/{type}/{id}", caption, null);
        }
    }
}