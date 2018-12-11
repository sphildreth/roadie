using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Models;
using Roadie.Library.Models.Collections;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Statistics;
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
        private IBookmarkService BookmarkService { get; } = null;

        public CollectionService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext dbContext,
                             ICacheManager cacheManager,
                             ILogger<CollectionService> logger,
                             IBookmarkService bookmarkService)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
            this.BookmarkService = bookmarkService;
        }

        public async Task<OperationResult<Collection>> ById(User roadieUser, Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = string.Format("urn:collection_by_id_operation:{0}:{1}", id, includes == null ? "0" : string.Join("|", includes));
            var result = await this.CacheManager.GetAsync<OperationResult<Collection>>(cacheKey, async () =>
            {
                return await this.CollectionByIdAction(id, includes);
            }, data.Artist.CacheRegionUrn(id));
            sw.Stop();
            if (result?.Data != null && roadieUser != null)
            {
                var userBookmarkResult = await this.BookmarkService.List(roadieUser, new PagedRequest(), false, BookmarkType.Collection);
                if (userBookmarkResult.IsSuccess)
                {
                    result.Data.UserBookmarked = userBookmarkResult?.Rows?.FirstOrDefault(x => x.Bookmark.Text == result.Data.Id.ToString()) != null;
                }
            }
            return new OperationResult<Collection>(result.Messages)
            {
                Data = result?.Data,
                IsNotFoundResult = result?.IsNotFoundResult ?? false,
                Errors = result?.Errors,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        private async Task<OperationResult<Collection>> CollectionByIdAction(Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();

            var collection = this.GetCollection(id);

            if (collection == null)
            {
                return new OperationResult<Collection>(true, string.Format("Collection Not Found [{0}]", id));
            }

            var result = collection.Adapt<Collection>();
            result.AlternateNames = collection.AlternateNames;
            result.Tags = collection.Tags;
            result.URLs = collection.URLs;
            result.Thumbnail = this.MakeCollectionThumbnailImage(collection.RoadieId);
            result.MediumThumbnail = base.MakeThumbnailImage(id, "collection", this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height);
            result.CollectionFoundCount = (from crc in this.DbContext.CollectionReleases
                                           where crc.CollectionId == collection.Id
                                           select crc.Id).Count();
            if (includes != null && includes.Any())
            {
                if (includes.Contains("list"))
                {
                    result.ListInCSVFormat = collection.ListInCSVFormat;
                    result.ListInCSV = collection.ListInCSV;
                }
                else
                {
                    result.ListInCSV = null;
                    result.ListInCSVFormat = null;
                }

                if (includes.Contains("releases"))
                {
                    result.Releases = (from crc in this.DbContext.CollectionReleases
                                       join r in this.DbContext.Releases.Include(x => x.Artist) on crc.ReleaseId equals r.Id
                                       where crc.CollectionId == collection.Id
                                       orderby crc.ListNumber
                                       select new CollectionRelease
                                       {
                                           ListNumber = crc.ListNumber,
                                           Release = Library.Models.Releases.ReleaseList.FromDataRelease(r, r.Artist, this.HttpContext.BaseUrl, this.MakeArtistThumbnailImage(r.Artist.RoadieId), this.MakeReleaseThumbnailImage(r.RoadieId))
                                       }).ToArray();
                }

                if (includes.Contains("stats"))
                {
                    var collectionReleases = (from crc in this.DbContext.CollectionReleases
                                              join r in this.DbContext.Releases.Include(x => x.Artist) on crc.ReleaseId equals r.Id
                                              where crc.CollectionId == collection.Id
                                              select r);

                    var collectionTracks = (from crc in this.DbContext.CollectionReleases
                                            join r in this.DbContext.Releases.Include(x => x.Artist) on crc.ReleaseId equals r.Id
                                            join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                            join t in this.DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                            where crc.CollectionId == collection.Id
                                            select t);

                    result.Statistics = new CollectionStatistics
                    {
                        ArtistCount = collectionReleases.Select(x => x.ArtistId).Distinct().Count(),
                        FileSize = collectionTracks.Sum(x => (long?)x.FileSize).ToFileSize(),
                        MissingTrackCount = collectionTracks.Count(x => x.Hash != null),
                        ReleaseCount = collectionReleases.Count(),
                        ReleaseMediaCount = collectionReleases.Sum(x => x.MediaCount),
                        Duration = collectionReleases.Sum(x => (long?)x.Duration),
                        TrackCount = collectionReleases.Sum(x => x.TrackCount),
                        TrackPlayedCount = collectionReleases.Sum(x => x.PlayedCount)
                    };
                }

            }

            sw.Stop();
            return new OperationResult<Collection>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            };

        }


        public async Task<Library.Models.Pagination.PagedResult<CollectionList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false, Guid? releaseId = null, Guid? artistId = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            IQueryable<data.Collection> collections = null;
            if(artistId.HasValue)
            {
                var sql = @"select DISTINCT c.*
                            from `collectionrelease` cr
                            join `collection` c on c.id = cr.collectionId
                            join `release` r on r.id = cr.releaseId
                            join `artist` a on r.artistId = a.id
                            where a.roadieId = {0}";

                collections = this.DbContext.Collections.FromSql(sql, artistId);
            }
            else if(releaseId.HasValue)
            {
                var sql = @"select DISTINCT c.*
                            from `collectionrelease` cr
                            join `collection` c on c.id = cr.collectionId
                            join `release` r on r.id = cr.releaseId
                            where r.roadieId = {0}";

                collections = this.DbContext.Collections.FromSql(sql, releaseId);
            }
            else
            {
                collections = this.DbContext.Collections;
            }
            var result = (from c in collections
                          where (request.FilterValue.Length == 0 || (request.FilterValue.Length > 0 && c.Name.Contains(request.Filter)))
                          select CollectionList.FromDataCollection(c, (from crc in this.DbContext.CollectionReleases
                                                                       where crc.CollectionId == c.Id
                                                                       select crc.Id).Count(), this.MakeCollectionThumbnailImage(c.RoadieId)));
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