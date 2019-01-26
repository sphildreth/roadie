using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Imaging;
using Roadie.Library.Models.Collections;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Statistics;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        /// <summary>
        /// Get blank Collection to add
        /// </summary>
        /// <param name="roadieUser"></param>
        /// <returns></returns>
        public OperationResult<Collection> Add(User roadieUser)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();

            var id = Guid.Empty;
            var collection = new data.Collection
            {
                Status = Statuses.New
            };
            var result = collection.Adapt<Collection>();
            result.Id = id;
            result.Thumbnail = this.MakeNewImage("collection");
            result.MediumThumbnail = this.MakeNewImage("collection");
            result.Maintainer = new Library.Models.DataToken
            {
                Value = roadieUser.UserId.ToString(),
                Text = roadieUser.UserName
            };
            sw.Stop();

            return new OperationResult<Collection>()
            {
                Data = result,
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        /// <summary>
        /// Updates (or Adds) Collection
        /// </summary>
        public async Task<OperationResult<bool>> UpdateCollection(User user, Collection model)
        {
            var isNew = model.Id == Guid.Empty;
            var now = DateTime.UtcNow;

            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();

            data.Collection collection = new data.Collection();

            if(!isNew)
            {
                collection = this.DbContext.Collections.FirstOrDefault(x => x.RoadieId == model.Id);
                if (collection == null)
                {
                    return new OperationResult<bool>(true, string.Format("Collection Not Found [{0}]", model.Id));
                }
            }
            collection.IsLocked = model.IsLocked;
            collection.Name = model.Name;
            collection.SortName = model.SortName;
            collection.Edition = model.Edition;
            collection.ListInCSVFormat = model.ListInCSVFormat;
            collection.ListInCSV = model.ListInCSV;
            collection.Description = model.Description;
            collection.URLs = model.URLs;
            collection.Status = SafeParser.ToEnum<Statuses>(model.Status);
            collection.CollectionType = SafeParser.ToEnum<CollectionType>(model.CollectionType);
            collection.Tags = model.TagsList.ToDelimitedList();
            collection.URLs = model.URLsList.ToDelimitedList();
            collection.AlternateNames = model.AlternateNamesList.ToDelimitedList();
            collection.CollectionCount = model.CollectionCount;

            var collectionImage = ImageHelper.ImageDataFromUrl(model.NewThumbnailData);
            if (collectionImage != null)
            {
                // Ensure is jpeg first
                collection.Thumbnail = ImageHelper.ConvertToJpegFormat(collectionImage);

                // Resize to store in database as thumbnail
                collection.Thumbnail = ImageHelper.ResizeImage(collection.Thumbnail, this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height);
            }

            if (model.Maintainer?.Value != null)
            {
                var maintainer = this.DbContext.Users.FirstOrDefault(x => x.RoadieId == SafeParser.ToGuid(model.Maintainer.Value));
                if (maintainer != null)
                {
                    collection.MaintainerId = maintainer.Id;
                }
            }
            collection.LastUpdated = now;

            if (isNew)
            {
                await this.DbContext.Collections.AddAsync(collection);
            }
            await this.DbContext.SaveChangesAsync();
            this.CacheManager.ClearRegion(collection.CacheRegion);
            this.Logger.LogInformation($"UpdateArtist `{ collection }` By User `{ user }`");
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = !errors.Any(),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteCollection(User user, Guid id)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var collection = this.DbContext.Collections.FirstOrDefault(x => x.RoadieId == id);
            if (collection == null)
            {
                return new OperationResult<bool>(true, $"Collection Not Found [{ id }]");
            }
            if(!user.IsEditor)
            {
                this.Logger.LogWarning($"DeleteCollection: Access Denied: `{ collection }`, By User `{user }`");
                return new OperationResult<bool>("Access Denied");
            }
            try
            {
                this.DbContext.Collections.Remove(collection);
                await this.DbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex);
                errors.Add(ex);
            }
            sw.Stop();
            this.Logger.LogInformation($"DeleteCollection `{ collection }`, By User `{user }`");
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
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

        public Task<Library.Models.Pagination.PagedResult<CollectionList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false, Guid? releaseId = null, Guid? artistId = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            IQueryable<data.Collection> collections = null;
            if (artistId.HasValue)
            {
                var sql = @"select DISTINCT c.*
                            from `collectionrelease` cr
                            join `collection` c on c.id = cr.collectionId
                            join `release` r on r.id = cr.releaseId
                            join `artist` a on r.artistId = a.id
                            where a.roadieId = {0}";

                collections = this.DbContext.Collections.FromSql(sql, artistId);
            }
            else if (releaseId.HasValue)
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
            return Task.FromResult(new Library.Models.Pagination.PagedResult<CollectionList>
            {
                TotalCount = rowCount,
                CurrentPage = request.PageValue,
                TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                OperationTime = sw.ElapsedMilliseconds,
                Rows = rows
            });
        }

        private Task<OperationResult<Collection>> CollectionByIdAction(Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();

            var collection = this.GetCollection(id);

            if (collection == null)
            {
                return Task.FromResult(new OperationResult<Collection>(true, string.Format("Collection Not Found [{0}]", id)));
            }

            var result = collection.Adapt<Collection>();
            var maintainer = this.DbContext.Users.FirstOrDefault(x => x.Id == collection.MaintainerId);
            result.Maintainer = new Library.Models.DataToken
            {
                Text = maintainer.UserName,
                Value = maintainer.RoadieId.ToString()
            };
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
            return Task.FromResult(new OperationResult<Collection>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            });
        }
    }
}