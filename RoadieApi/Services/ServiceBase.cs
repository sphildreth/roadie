using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Identity;
using Roadie.Library.Models;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
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

        protected Image MakeImage(Guid id, int width = 200, int height = 200)
        {
            return new Image($"{this.HttpContext.ImageBaseUrl }/{id}/{ width }/{ height }");
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

        private Image MakeImage(Guid id, string type, int? width, int? height)
        {
            if (width.HasValue && height.HasValue)
            {
                return new Image($"{this.HttpContext.ImageBaseUrl }/{type}/{id}/{width}/{height}");
            }
            return new Image($"{this.HttpContext.ImageBaseUrl }/{type}/{id}");
        }

        private Image MakeThumbnailImage(Guid id, string type)
        {
            return this.MakeImage(id, type, this.Configuration.ThumbnailImageSize.Width, this.Configuration.ThumbnailImageSize.Height);
        }
    }
}