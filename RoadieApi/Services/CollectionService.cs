using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Models;
using Roadie.Library.Models.Collections;
using Roadie.Library.Models.Pagination;
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

        public async Task<Library.Models.Pagination.PagedResult<CollectionList>> CollectionList(PagedRequest request, Guid? releaseId = null, Guid? artistId = null)
        {
            var sw = new Stopwatch();
            sw.Start();

            if (!string.IsNullOrEmpty(request.Sort))
            {
                request.Sort = request.Sort.Replace("createdDate", "createdDateTime");
                request.Sort = request.Sort.Replace("lastUpdated", "lastUpdatedDateTime");
            }
            var result = (from c in this.DbContext.Collections
                          join cr in this.DbContext.CollectionReleases on c.Id equals cr.CollectionId into crs
                          from cr in crs.DefaultIfEmpty()
                          join r in this.DbContext.Releases on cr.ReleaseId equals r.Id into rs
                          from r in rs.DefaultIfEmpty()
                          join a in this.DbContext.Artists on r.ArtistId equals a.Id into aas
                          from a in aas.DefaultIfEmpty()
                          where (releaseId == null || (releaseId != null && r.RoadieId == releaseId))
                          where (artistId == null || (artistId != null && a.RoadieId == artistId))
                          where (request.FilterValue.Length == 0 || (request.FilterValue.Length > 0 && (
                                    c.Name != null && c.Name.ToLower().Contains(request.Filter.ToLower()))
                          ))
                          select new CollectionList
                          {
                              Collection = new DataToken
                              {
                                  Text = c.Name,
                                  Value = c.RoadieId.ToString()
                              },
                              Release = new DataToken
                              {
                                  Text = artistId != null && r != null ? r.Title : null,
                                  Value = artistId != null && r != null ? r.RoadieId.ToString() : null
                              },
                              Id = c.RoadieId,
                              CollectionCount = c.CollectionCount,
                              CollectionType = (c.CollectionType ?? CollectionType.Unknown).ToString(),
                              CollectionFoundCount = (from crc in this.DbContext.CollectionReleases
                                                      where crc.CollectionId == c.Id
                                                      select crc.Id).Count(),
                              CollectionPosition = (releaseId != null || artistId != null) ? (int?)cr.ListNumber : null,
                              CreatedDate = c.CreatedDate,
                              LastUpdated = c.LastUpdated,
                              Thumbnail = MakeCollectionThumbnailImage(c.RoadieId)
                          }).Distinct();
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