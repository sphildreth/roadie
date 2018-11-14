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
                                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Release.CacheRegionUrn(id));
        }

        protected data.Track GetTrack(Guid id)
        {
            return this.CacheManager.Get(data.Track.CacheUrn(id), () =>
            {
                return this.DbContext.Tracks
                                    .FirstOrDefault(x => x.RoadieId == id);
            }, data.Track.CacheRegionUrn(id));
        }

        protected ApplicationUser GetUser(Guid id)
        {
            return this.CacheManager.Get(ApplicationUser.CacheUrn(id), () =>
            {
                return this.DbContext.Users
                                    .Include(x => x.ArtistRatings)
                                    .Include(x => x.ReleaseRatings)
                                    .Include(x => x.TrackRatings)
                                    .FirstOrDefault(x => x.RoadieId == id);
            }, ApplicationUser.CacheRegionUrn(id));
        }

        protected List<BookmarkList> GetUserBookmarks(User roadieUser)
        {
            return this.CacheManager.Get($"urn:user_bookmarks:{ roadieUser.Id }", () =>
            {
                var bookmarks = from b in this.DbContext.Bookmarks
                                join a in this.DbContext.Artists on b.BookmarkTargetId equals a.Id into aa
                                from a in aa.DefaultIfEmpty()
                                join r in this.DbContext.Releases on b.BookmarkTargetId equals r.Id into rr
                                from r in rr.DefaultIfEmpty()
                                join t in this.DbContext.Tracks on b.BookmarkTargetId equals t.Id into tt
                                from t in tt.DefaultIfEmpty()
                                join p in this.DbContext.Playlists on b.BookmarkTargetId equals p.Id into pp
                                from p in pp.DefaultIfEmpty()
                                join c in this.DbContext.Collections on b.BookmarkTargetId equals c.Id into cc
                                from c in cc.DefaultIfEmpty()
                                join l in this.DbContext.Labels on b.BookmarkTargetId equals l.Id into ll
                                from l in ll.DefaultIfEmpty()
                                where b.UserId == roadieUser.Id
                                select new
                                {
                                    b,
                                    a,
                                    r,
                                    t,
                                    p,
                                    c,
                                    l
                                };

                var result = new List<BookmarkList>();
                foreach (var bookmark in bookmarks)
                {
                    var b = bookmark.b.Adapt<BookmarkList>();
                    if (bookmark.a != null)
                    {
                        b.Bookmark = new DataToken
                        {
                            Text = bookmark.a.Name,
                            Value = bookmark.a.RoadieId.ToString()
                        };
                        b.Thumbnail = this.MakeArtistThumbnailImage(bookmark.a.RoadieId);
                        continue;
                    }
                    if (bookmark.r != null)
                    {
                        b.Bookmark = new DataToken
                        {
                            Text = bookmark.r.Title,
                            Value = bookmark.r.RoadieId.ToString()
                        };
                        b.Thumbnail = this.MakeReleaseThumbnailImage(bookmark.r.RoadieId);
                        continue;
                    }
                    if (bookmark.t != null)
                    {
                        b.Bookmark = new DataToken
                        {
                            Text = bookmark.t.Title,
                            Value = bookmark.t.RoadieId.ToString()
                        };
                        b.Thumbnail = this.MakeTrackThumbnailImage(bookmark.t.RoadieId);
                        continue;
                    }
                    if (bookmark.p != null)
                    {
                        b.Bookmark = new DataToken
                        {
                            Text = bookmark.p.Name,
                            Value = bookmark.p.RoadieId.ToString()
                        };
                        b.Thumbnail = this.MakePlaylistThumbnailImage(bookmark.p.RoadieId);
                        continue;
                    }
                    if (bookmark.c != null)
                    {
                        b.Bookmark = new DataToken
                        {
                            Text = bookmark.c.Name,
                            Value = bookmark.c.RoadieId.ToString()
                        };
                        b.Thumbnail = this.MakeCollectionThumbnailImage(bookmark.c.RoadieId);
                        continue;
                    }
                    if (bookmark.l != null)
                    {
                        b.Bookmark = new DataToken
                        {
                            Text = bookmark.l.Name,
                            Value = bookmark.l.RoadieId.ToString()
                        };
                        b.Thumbnail = this.MakeLabelThumbnailImage(bookmark.l.RoadieId);
                        continue;
                    }
                    result.Add(b);
                }
                return result;
            }, ApplicationUser.CacheRegionUrn(roadieUser.UserId));
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

        protected Image MakeLabelThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "label");
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

        private Image MakeThumbnailImage(Guid id, string type)
        {
            return new Image($"{this.HttpContext.ImageBaseUrl }/thumbnail/{ type }/{id}");
        }
    }
}