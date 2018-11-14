using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Models;
using Roadie.Library.Models.Collections;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public class CollectionService : ServiceBase, ICollectionService
    {
        public CollectionService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext dbContext,
                             ICacheManager cacheManager,
                             ILogger<CollectionService> logger)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
        }

        public async Task<Library.Models.Pagination.PagedResult<CollectionList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false, Guid? releaseId = null, Guid? artistId = null)
        {
            var sw = new Stopwatch();
            sw.Start();

            if (!string.IsNullOrEmpty(request.Sort))
            {
                request.Sort = request.Sort.Replace("createdDate", "createdDateTime");
                request.Sort = request.Sort.Replace("lastUpdated", "lastUpdatedDateTime");
            }
            var result = (from c in this.DbContext.Collections
                          where (request.FilterValue.Length == 0 || (request.FilterValue.Length > 0 && c.Name.Contains(request.Filter)))
                          select new CollectionList
                          {
                              DatabaseId = c.Id,
                              Collection = new DataToken
                              {
                                  Text = c.Name,
                                  Value = c.RoadieId.ToString()
                              },
                              Id = c.RoadieId,
                              CollectionCount = c.CollectionCount,
                              CollectionType = (c.CollectionType ?? CollectionType.Unknown).ToString(),
                              CollectionFoundCount = (from crc in this.DbContext.CollectionReleases
                                                      where crc.CollectionId == c.Id
                                                      select crc.Id).Count(),
                              CreatedDate = c.CreatedDate,
                              LastUpdated = c.LastUpdated,
                              Thumbnail = MakeCollectionThumbnailImage(c.RoadieId)
                          });
            if(artistId.HasValue || releaseId.HasValue)
            {
                result = (from re in result
                          join cr in this.DbContext.CollectionReleases on re.DatabaseId equals cr.CollectionId into crs
                          from cr in crs.DefaultIfEmpty()
                          join r in this.DbContext.Releases on cr.ReleaseId equals r.Id into rs
                          from r in rs.DefaultIfEmpty()
                          join a in this.DbContext.Artists on r.ArtistId equals a.Id into aas
                          from a in aas.DefaultIfEmpty()
                          where (releaseId == null || r.RoadieId == releaseId)
                          where (artistId == null ||  a.RoadieId == artistId)
                          select re);
            }
            var sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "Collection.Text", "ASC" } }) : request.OrderValue(null);
            var rowCount = result.Count();
            var rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
            sw.Stop();
            return new Library.Models.Pagination.PagedResult<CollectionList>
            {
                TotalCount = rowCount,
                CurrentPage = request.PageValue,
                TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                OperationTime = sw.ElapsedMilliseconds,
                Rows = rows
            };
        }
    }
}