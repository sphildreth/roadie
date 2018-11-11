using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using data = Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Roadie.Library.Utility;
using Microsoft.EntityFrameworkCore;

namespace Roadie.Api.Services
{
    public abstract class ServiceBase
    {
        protected readonly ICacheManager _cacheManager = null;
        protected readonly IRoadieSettings _configuration = null;
        protected readonly data.IRoadieDbContext _dbContext = null;
        protected readonly IHttpEncoder _httpEncoder = null;
        protected readonly ILogger _logger = null;
        protected readonly IHttpContext _httpContext = null;

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

        protected IHttpContext HttpContext
        {
            get
            {
                return this._httpContext;
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

        protected Image MakeArtistThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "artist");
        }

        protected Image MakeCollectionThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "collection");
        }

        protected Image MakePlaylistThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "playlist");
        }

        protected Image MakeImage(Guid id, int width = 200, int height = 200)
        {
            return new Image($"{this.HttpContext.ImageBaseUrl }/{id}/{ width }/{ height }");
        }

        protected Image MakeReleaseThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "release");
        }

        protected Image MakeLabelThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "label");
        }

        protected Image MakeUserThumbnailImage(Guid id)
        {
            return MakeThumbnailImage(id, "user");
        }

        private Image MakeThumbnailImage(Guid id, string type)
        {
            return new Image($"{this.HttpContext.ImageBaseUrl }/{ type }/thumbnail/{id}");
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
    }
}
