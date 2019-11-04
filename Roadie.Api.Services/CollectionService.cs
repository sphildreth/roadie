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
using Roadie.Library.Models;
using Roadie.Library.Models.Collections;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Releases;
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
        private IBookmarkService BookmarkService { get; }

        public CollectionService(IRoadieSettings configuration,
                    IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            data.IRoadieDbContext dbContext,
            ICacheManager cacheManager,
            ILogger<CollectionService> logger,
            IBookmarkService bookmarkService)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
            BookmarkService = bookmarkService;
        }

        /// <summary>
        ///     Get blank Collection to add
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
            result.Thumbnail = MakeNewImage("collection");
            result.MediumThumbnail = MakeNewImage("collection");
            result.Maintainer = new DataToken
            {
                Value = roadieUser.UserId.ToString(),
                Text = roadieUser.UserName
            };
            sw.Stop();

            return new OperationResult<Collection>
            {
                Data = result,
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<Collection>> ById(User roadieUser, Guid id,
            IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();

            var cacheKey = string.Format("urn:collection_by_id_operation:{0}:{1}", id, includes == null ? "0" : string.Join("|", includes));
            var result = await CacheManager.GetAsync(cacheKey, async () =>
            {
                return await CollectionByIdAction(id, includes);
            }, data.Artist.CacheRegionUrn(id));
            sw.Stop();
            if (result?.Data != null && roadieUser != null)
            {
                var userBookmarkResult = await BookmarkService.List(roadieUser, new PagedRequest(), false, BookmarkType.Collection);
                if (userBookmarkResult.IsSuccess)
                {
                    result.Data.UserBookmarked = userBookmarkResult?.Rows?.FirstOrDefault(x => x.Bookmark.Text == result.Data.Id.ToString()) != null;
                }
                if (result.Data.Comments.Any())
                {
                    var commentIds = result.Data.Comments.Select(x => x.DatabaseId).ToArray();
                    var userCommentReactions = (from cr in DbContext.CommentReactions
                                                where commentIds.Contains(cr.CommentId)
                                                where cr.UserId == roadieUser.Id
                                                select cr).ToArray();
                    foreach (var comment in result.Data.Comments)
                    {
                        var userCommentReaction =
                            userCommentReactions.FirstOrDefault(x => x.CommentId == comment.DatabaseId);
                        comment.IsDisliked = userCommentReaction?.ReactionValue == CommentReaction.Dislike;
                        comment.IsLiked = userCommentReaction?.ReactionValue == CommentReaction.Like;
                    }
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

        public async Task<OperationResult<bool>> DeleteCollection(User user, Guid id)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var collection = DbContext.Collections.FirstOrDefault(x => x.RoadieId == id);
            if (collection == null) return new OperationResult<bool>(true, $"Collection Not Found [{id}]");
            if (!user.IsEditor)
            {
                Logger.LogWarning($"DeleteCollection: Access Denied: `{collection}`, By User `{user}`");
                var r = new OperationResult<bool>("Access Denied");
                r.IsAccessDeniedResult = true;
                return r;
            }

            try
            {
                DbContext.Collections.Remove(collection);
                await DbContext.SaveChangesAsync();
                var collectionImageFilename = collection.PathToImage(Configuration);
                if (File.Exists(collectionImageFilename))
                {
                    File.Delete(collectionImageFilename);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                errors.Add(ex);
            }

            sw.Stop();
            Logger.LogWarning($"DeleteCollection `{collection}`, By User `{user}`");
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public Task<Library.Models.Pagination.PagedResult<CollectionList>> List(User roadieUser, PagedRequest request,
            bool? doRandomize = false, Guid? releaseId = null, Guid? artistId = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            IQueryable<data.Collection> collections = null;
            if (artistId.HasValue)
            {
                collections = (from cr in DbContext.CollectionReleases
                               join c in DbContext.Collections on cr.CollectionId equals c.Id
                               join r in DbContext.Releases on cr.ReleaseId equals r.Id
                               join a in DbContext.Artists on r.ArtistId equals a.Id
                               where a.RoadieId == artistId
                               select c).Distinct();
            }
            else if (releaseId.HasValue)
            {
                collections = (from cr in DbContext.CollectionReleases
                               join c in DbContext.Collections on cr.CollectionId equals c.Id
                               join r in DbContext.Releases on cr.ReleaseId equals r.Id
                               where r.RoadieId == releaseId
                               select c).Distinct();
            }
            else
            {
                collections = DbContext.Collections;
            }

            var result = from c in collections
                         let collectionCount = (from crc in DbContext.CollectionReleases
                                                where crc.CollectionId == c.Id
                                                select crc.Id).Count()
                         where request.FilterValue.Length == 0 ||
                               request.FilterValue.Length > 0 && c.Name.Contains(request.Filter)
                         where request.FilterToStatusValue == Statuses.Ok || c.Status == request.FilterToStatusValue
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
                             CollectionFoundCount = collectionCount,
                             CreatedDate = c.CreatedDate,
                             IsLocked = c.IsLocked,
                             LastUpdated = c.LastUpdated,
                             Thumbnail = MakeCollectionThumbnailImage(Configuration, HttpContext, c.RoadieId)
                         };

            var sortBy = string.IsNullOrEmpty(request.Sort)
                ? request.OrderValue(new Dictionary<string, string> { { "Collection.Text", "ASC" } })
                : request.OrderValue();
            var rowCount = result.Count();
            var rows = result
                       .OrderBy(sortBy)
                       .Skip(request.SkipValue)
                       .Take(request.LimitValue)
                       .ToArray();
            if (request.FilterToStatusValue == Statuses.Incomplete)
            {
                rows = rows.OrderByDescending(x => x.PercentComplete).ThenBy(x => x.SortName).ToArray();
            }
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

        /// <summary>
        ///     Updates (or Adds) Collection
        /// </summary>
        public async Task<OperationResult<bool>> UpdateCollection(User user, Collection model)
        {
            var isNew = model.Id == Guid.Empty;
            var now = DateTime.UtcNow;

            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();

            var collection = new data.Collection();

            if (!isNew)
            {
                collection = DbContext.Collections.FirstOrDefault(x => x.RoadieId == model.Id);
                if (collection == null)
                {
                    return new OperationResult<bool>(true, string.Format("Collection Not Found [{0}]", model.Id));
                }
                // If collection is being renamed, see if collection already exists with new model supplied name
                if (collection.Name.ToAlphanumericName() != model.Name.ToAlphanumericName())
                {
                    var existingCollection = DbContext.Collections.FirstOrDefault(x => x.Name == model.Name);
                    if (existingCollection != null)
                    {
                        return new OperationResult<bool>($"Collection already exists with name [{ model.Name }].");
                    }
                }
            }
            collection.IsLocked = model.IsLocked;
            var oldPathToImage = collection.PathToImage(Configuration);
            var didChangeName = collection.Name != model.Name;
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

            if (didChangeName)
            {
                if (File.Exists(oldPathToImage))
                {
                    File.Move(oldPathToImage, collection.PathToImage(Configuration));
                }
            }

            var collectionImage = ImageHelper.ImageDataFromUrl(model.NewThumbnailData);
            if (collectionImage != null)
            {
                // Save unaltered collection image
                File.WriteAllBytes(collection.PathToImage(Configuration), ImageHelper.ConvertToJpegFormat(collectionImage));
            }
            if (model.Maintainer?.Value != null)
            {
                var maintainer = DbContext.Users.FirstOrDefault(x => x.RoadieId == SafeParser.ToGuid(model.Maintainer.Value));
                if (maintainer != null) collection.MaintainerId = maintainer.Id;
            }

            collection.LastUpdated = now;

            if (isNew) await DbContext.Collections.AddAsync(collection);
            await DbContext.SaveChangesAsync();
            CacheManager.ClearRegion(collection.CacheRegion);
            Logger.LogInformation($"UpdateArtist `{collection}` By User `{user}`");
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = !errors.Any(),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        private Task<OperationResult<Collection>> CollectionByIdAction(Guid id, IEnumerable<string> includes = null)
        {
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            var sw = Stopwatch.StartNew();
            sw.Start();

            tsw.Restart();
            var collection = GetCollection(id);
            tsw.Stop();
            timings.Add("getCollection", tsw.ElapsedMilliseconds);

            if (collection == null)
            {
                return Task.FromResult(new OperationResult<Collection>(true, string.Format("Collection Not Found [{0}]", id)));
            }
            var result = collection.Adapt<Collection>();
            var maintainer = DbContext.Users.FirstOrDefault(x => x.Id == collection.MaintainerId);
            result.Maintainer = new DataToken
            {
                Text = maintainer.UserName,
                Value = maintainer.RoadieId.ToString()
            };
            result.AlternateNames = collection.AlternateNames;
            result.Tags = collection.Tags;
            result.URLs = collection.URLs;
            result.Thumbnail = MakeCollectionThumbnailImage(Configuration, HttpContext, collection.RoadieId);
            result.MediumThumbnail = MakeThumbnailImage(Configuration, HttpContext, id, "collection", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height);
            result.CollectionFoundCount = (from crc in DbContext.CollectionReleases
                                           where crc.CollectionId == collection.Id
                                           select crc.Id).Count();
            tsw.Stop();
            timings.Add("adapt", tsw.ElapsedMilliseconds);
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
                    tsw.Restart();
                    result.Releases = (from crc in DbContext.CollectionReleases
                                       join r in DbContext.Releases.Include(x => x.Artist) on crc.ReleaseId equals r.Id
                                       where crc.CollectionId == collection.Id
                                       orderby crc.ListNumber
                                       select new CollectionRelease
                                       {
                                           ListNumber = crc.ListNumber,
                                           Release = ReleaseList.FromDataRelease(r, r.Artist, HttpContext.BaseUrl, MakeArtistThumbnailImage(Configuration, HttpContext, r.Artist.RoadieId), MakeReleaseThumbnailImage(Configuration, HttpContext, r.RoadieId))
                                       }).ToArray();
                    tsw.Stop();
                    timings.Add("releases", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("stats"))
                {
                    tsw.Restart();
                    var collectionReleases = from crc in DbContext.CollectionReleases
                                             join r in DbContext.Releases on crc.ReleaseId equals r.Id
                                             where crc.CollectionId == collection.Id
                                             select r;

                    var collectionTracks = from crc in DbContext.CollectionReleases
                                           join r in DbContext.Releases on crc.ReleaseId equals r.Id
                                           join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                           join t in DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                           where crc.CollectionId == collection.Id
                                           select t;

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
                    tsw.Stop();
                    timings.Add("stats", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("comments"))
                {
                    tsw.Restart();
                    var collectionComments = DbContext.Comments.Include(x => x.User)
                                                      .Where(x => x.CollectionId == collection.Id)
                                                      .OrderByDescending(x => x.CreatedDate)
                                                      .ToArray();
                    if (collectionComments.Any())
                    {
                        var comments = new List<Comment>();
                        var commentIds = collectionComments.Select(x => x.Id).ToArray();
                        var userCommentReactions = (from cr in DbContext.CommentReactions
                                                    where commentIds.Contains(cr.CommentId)
                                                    select cr).ToArray();
                        foreach (var collectionComment in collectionComments)
                        {
                            var comment = collectionComment.Adapt<Comment>();
                            comment.DatabaseId = collectionComment.Id;
                            comment.User = UserList.FromDataUser(collectionComment.User, MakeUserThumbnailImage(Configuration, HttpContext, collectionComment.User.RoadieId));
                            comment.DislikedCount = userCommentReactions.Count(x => x.CommentId == collectionComment.Id && x.ReactionValue == CommentReaction.Dislike);
                            comment.LikedCount = userCommentReactions.Count(x => x.CommentId == collectionComment.Id && x.ReactionValue == CommentReaction.Like);
                            comments.Add(comment);
                        }
                        result.Comments = comments;
                    }
                    tsw.Stop();
                    timings.Add("comments", tsw.ElapsedMilliseconds);
                }
            }
            Logger.LogInformation($"ByIdAction: Collection `{ collection }`: includes [{includes.ToCSV()}], timings: [{ timings.ToTimings() }]");
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