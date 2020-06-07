using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Imaging;
using Roadie.Library.Models;
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
    public class LabelService : ServiceBase, ILabelService
    {
        private IBookmarkService BookmarkService { get; }

        public LabelService(IRoadieSettings configuration,
                    IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            IRoadieDbContext context,
            ICacheManager cacheManager,
            ILogger<LabelService> logger,
            IBookmarkService bookmarkService)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
            BookmarkService = bookmarkService;
        }

        private async Task<OperationResult<Label>> LabelByIdAction(Guid id, IEnumerable<string> includes = null)
        {
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            var sw = Stopwatch.StartNew();
            sw.Start();

            tsw.Restart();
            var label = await GetLabel(id).ConfigureAwait(false);
            tsw.Stop();
            timings.Add(nameof(GetLabel), tsw.ElapsedMilliseconds);
            if (label == null)
            {
                return new OperationResult<Label>(true, $"Label Not Found [{id}]");
            }
            tsw.Restart();
            var result = label.Adapt<Label>();
            result.AlternateNames = label.AlternateNames;
            result.Tags = label.Tags;
            result.URLs = label.URLs;
            result.Thumbnail = ImageHelper.MakeLabelThumbnailImage(Configuration, HttpContext, label.RoadieId);
            result.MediumThumbnail = ImageHelper.MakeThumbnailImage(Configuration, HttpContext, id, "label", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height);
            tsw.Stop();
            timings.Add("adapt", tsw.ElapsedMilliseconds);
            if (includes != null && includes.Any())
            {
                if (includes.Contains("stats"))
                {
                    tsw.Restart();
                    var labelTracks = from l in DbContext.Labels
                                      join rl in DbContext.ReleaseLabels on l.Id equals rl.LabelId into rld
                                      from rl in rld.DefaultIfEmpty()
                                      join r in DbContext.Releases on rl.ReleaseId equals r.Id
                                      join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                      join t in DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                      where l.Id == label.Id
                                      select new
                                      {
                                          t.Duration,
                                          t.FileSize
                                      };
                    result.Duration = labelTracks.Sum(x => x.Duration);

                    result.Statistics = new ReleaseGroupingStatistics
                    {
                        TrackCount = label.TrackCount,
                        ArtistCount = label.ArtistCount,
                        ReleaseCount = label.ReleaseCount,
                        TrackSize = result.DurationTime,
                        FileSize = labelTracks.Sum(x => (long?)x.FileSize).ToFileSize()
                    };
                    tsw.Stop();
                    timings.Add("stats", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("comments"))
                {
                    tsw.Restart();
                    var labelComments = DbContext.Comments.Include(x => x.User)
                                                  .Where(x => x.LabelId == label.Id)
                                                  .OrderByDescending(x => x.CreatedDate)
                                                  .ToArray();
                    if (labelComments.Any())
                    {
                        var comments = new List<Comment>();
                        var commentIds = labelComments.Select(x => x.Id).ToArray();
                        var userCommentReactions = (from cr in DbContext.CommentReactions
                                                    where commentIds.Contains(cr.CommentId)
                                                    select cr).ToArray();
                        foreach (var labelComment in labelComments)
                        {
                            var comment = labelComment.Adapt<Comment>();
                            comment.DatabaseId = labelComment.Id;
                            comment.User = UserList.FromDataUser(labelComment.User, ImageHelper.MakeUserThumbnailImage(Configuration, HttpContext, labelComment.User.RoadieId));
                            comment.DislikedCount = userCommentReactions.Count(x => x.CommentId == labelComment.Id && x.ReactionValue == CommentReaction.Dislike);
                            comment.LikedCount = userCommentReactions.Count(x => x.CommentId == labelComment.Id && x.ReactionValue == CommentReaction.Like);
                            comments.Add(comment);
                        }
                        result.Comments = comments;
                    }
                    tsw.Stop();
                    timings.Add("comments", tsw.ElapsedMilliseconds);
                }
            }

            sw.Stop();
            Logger.LogInformation($"ByIdAction: Label `{ label }`: includes [{includes.ToCSV()}], timings: [{ timings.ToTimings() }]");
            return new OperationResult<Label>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        private async Task<OperationResult<Library.Models.Image>> SaveImageBytes(Library.Models.Users.User user, Guid id, byte[] imageBytes)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var label = await DbContext.Labels.FirstOrDefaultAsync(x => x.RoadieId == id).ConfigureAwait(false);
            if (label == null)
            {
                return new OperationResult<Library.Models.Image>(true, $"Label Not Found [{id}]");
            }

            try
            {
                var now = DateTime.UtcNow;
                if (imageBytes != null)
                {
                    // Save unaltered label image
                    File.WriteAllBytes(label.PathToImage(Configuration, true), ImageHelper.ConvertToJpegFormat(imageBytes));
                }
                label.LastUpdated = now;
                await DbContext.SaveChangesAsync().ConfigureAwait(false);
                CacheManager.ClearRegion(label.CacheRegion);
                Logger.LogInformation($"UploadLabelImage `{label}` By User `{user}`");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                errors.Add(ex);
            }

            sw.Stop();

            return new OperationResult<Library.Models.Image>
            {
                IsSuccess = errors.Count == 0,
                Data = ImageHelper.MakeThumbnailImage(Configuration, HttpContext, id, "label", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height, true),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<Label>> ByIdAsync(Library.Models.Users.User roadieUser, Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = $"urn:label_by_id_operation:{id}:{(includes == null ? "0" : string.Join("|", includes))}";
            var result = await CacheManager.GetAsync(cacheKey,
                async () => await LabelByIdAction(id, includes).ConfigureAwait(false), data.Label.CacheRegionUrn(id)
            ).ConfigureAwait(false);
            sw.Stop();
            if (result?.Data != null && roadieUser != null)
            {
                var userBookmarkResult = await BookmarkService.ListAsync(roadieUser, new PagedRequest(), false, BookmarkType.Label).ConfigureAwait(false);
                if (userBookmarkResult.IsSuccess)
                {
                    result.Data.UserBookmarked = userBookmarkResult?.Rows?.FirstOrDefault(x => x?.Bookmark?.Value == result?.Data?.Id?.ToString()) != null;
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
                        var userCommentReaction = userCommentReactions.FirstOrDefault(x => x.CommentId == comment.DatabaseId);
                        comment.IsDisliked = userCommentReaction?.ReactionValue == CommentReaction.Dislike;
                        comment.IsLiked = userCommentReaction?.ReactionValue == CommentReaction.Like;
                    }
                }
            }

            return new OperationResult<Label>(result.Messages)
            {
                Data = result?.Data,
                IsNotFoundResult = result?.IsNotFoundResult ?? false,
                Errors = result?.Errors,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> DeleteAsync(Library.Identity.User user, Guid id)
        {
            var sw = new Stopwatch();
            sw.Start();
            var label = await DbContext.Labels.FirstOrDefaultAsync(x => x.RoadieId == id).ConfigureAwait(false);
            if (label == null)
            {
                return new OperationResult<bool>(true, $"Label Not Found [{id}]");
            }

            DbContext.Labels.Remove(label);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            var labelImageFilename = label.PathToImage(Configuration);
            if (File.Exists(labelImageFilename))
            {
                File.Delete(labelImageFilename);
            }
            await BookmarkService.RemoveAllBookmarksForItemAsync(BookmarkType.Label, label.Id).ConfigureAwait(false);
            Logger.LogWarning("User `{0}` deleted Label `{1}]`", user, label);
            CacheManager.ClearRegion(label.CacheRegion);
            sw.Stop();
            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<Library.Models.Pagination.PagedResult<LabelList>> ListAsync(Library.Models.Users.User roadieUser, PagedRequest request,
            bool? doRandomize = false)
        {
            var sw = new Stopwatch();
            sw.Start();

            int? rowCount = null;

            if (!string.IsNullOrEmpty(request.Sort))
            {
                request.Sort = request.Sort.Replace("createdDate", "createdDateTime");
                request.Sort = request.Sort.Replace("lastUpdated", "lastUpdatedDateTime");
            }

            var normalizedFilterValue = !string.IsNullOrEmpty(request.FilterValue)
                ? request.FilterValue.ToAlphanumericName()
                : null;

            int[] randomLabelIds = null;
            SortedDictionary<int, int> randomLabelData = null;
            if (doRandomize ?? false)
            {
                var randomLimit = request.Limit ?? roadieUser?.RandomReleaseLimit ?? request.LimitValue;
                randomLabelData = await DbContext.RandomLabelIdsAsync(roadieUser?.Id ?? -1, randomLimit, request.FilterFavoriteOnly, request.FilterRatedOnly).ConfigureAwait(false);
                randomLabelIds = randomLabelData.Select(x => x.Value).ToArray();
                rowCount = DbContext.Labels.Count();
            }

            var result = from l in DbContext.Labels
                         where randomLabelIds == null || randomLabelIds.Contains(l.Id)
                         where string.IsNullOrEmpty(normalizedFilterValue) || (
                                   l.Name.ToLower().Contains(normalizedFilterValue) ||
                                   l.SortName.ToLower().Contains(normalizedFilterValue) ||
                                   l.AlternateNames.ToLower().Contains(normalizedFilterValue)
                               )
                         select new LabelList
                         {
                             DatabaseId = l.Id,
                             Id = l.RoadieId,
                             Label = new DataToken
                             {
                                 Text = l.Name,
                                 Value = l.RoadieId.ToString()
                             },
                             SortName = l.SortName,
                             CreatedDate = l.CreatedDate,
                             LastUpdated = l.LastUpdated,
                             ArtistCount = l.ArtistCount,
                             ReleaseCount = l.ReleaseCount,
                             TrackCount = l.TrackCount,
                             Thumbnail = ImageHelper.MakeLabelThumbnailImage(Configuration, HttpContext, l.RoadieId)
                         };
            LabelList[] rows = null;
            rowCount = rowCount ?? result.Count();
            if (doRandomize ?? false)
            {
                var resultData = await result.ToArrayAsync().ConfigureAwait(false);
                rows = (from r in resultData
                              join ra in randomLabelData on r.DatabaseId equals ra.Value
                              orderby ra.Key
                              select r
                       )
                       .ToArray();
            }
            else
            {
                var sortBy = string.IsNullOrEmpty(request.Sort)
                    ? request.OrderValue(new Dictionary<string, string> { { "SortName", "ASC" }, { "Label.Text", "ASC" } })
                    : request.OrderValue();
                rows = await result
                            .OrderBy(sortBy)
                            .Skip(request.SkipValue)
                            .Take(request.LimitValue)
                            .ToArrayAsync()
                            .ConfigureAwait(false);
            }

            sw.Stop();
            return new Library.Models.Pagination.PagedResult<LabelList>
            {
                TotalCount = rowCount.Value,
                CurrentPage = request.PageValue,
                TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                OperationTime = sw.ElapsedMilliseconds,
                Rows = rows
            };
        }

        public async Task<OperationResult<bool>> MergeLabelsIntoLabelAsync(Library.Identity.User user, Guid intoLabelId, IEnumerable<Guid> labelIdsToMerge)
        {
            var sw = new Stopwatch();
            sw.Start();

            var errors = new List<Exception>();
            var label = await DbContext.Labels.FirstOrDefaultAsync(x => x.RoadieId == intoLabelId).ConfigureAwait(false);
            if (label == null)
            {
                return new OperationResult<bool>(true, $"Merge Into Label Not Found [{intoLabelId}]");
            }

            var now = DateTime.UtcNow;
            var labelsToMerge = (from l in DbContext.Labels
                                 join ltm in labelIdsToMerge on l.RoadieId equals ltm
                                 select l);
            foreach (var labelToMerge in labelsToMerge)
            {
                label.MusicBrainzId = label.MusicBrainzId ?? labelToMerge.MusicBrainzId;
                label.SortName = label.SortName ?? labelToMerge.SortName;
                label.Profile = label.Profile ?? labelToMerge.Profile;
                label.BeginDate = label.BeginDate ?? labelToMerge.BeginDate;
                label.EndDate = label.EndDate ?? labelToMerge.EndDate;
                label.Profile = label.Profile ?? labelToMerge.Profile;
                label.DiscogsId = label.DiscogsId ?? labelToMerge.DiscogsId;
                label.ImageUrl = label.ImageUrl ?? labelToMerge.ImageUrl;
                label.Tags = label.Tags.AddToDelimitedList(labelToMerge.Tags.ToListFromDelimited());
                var altNames = labelToMerge.AlternateNames.ToListFromDelimited().ToList();
                altNames.Add(labelToMerge.Name);
                altNames.Add(labelToMerge.SortName);
                altNames.Add(labelToMerge.Name.ToAlphanumericName());
                label.AlternateNames = label.AlternateNames.AddToDelimitedList(altNames);
                label.URLs = label.URLs.AddToDelimitedList(labelToMerge.URLs.ToListFromDelimited());

                var labelToMergeReleases = (from rl in DbContext.ReleaseLabels
                                            where rl.LabelId == labelToMerge.Id
                                            select rl);
                foreach (var labelToMergeRelease in labelToMergeReleases)
                {
                    labelToMergeRelease.LabelId = label.Id;
                    labelToMergeRelease.LastUpdated = now;
                }
                label.LastUpdated = now;
                await DbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            await UpdateLabelCounts(label.Id, now).ConfigureAwait(false);

            CacheManager.ClearRegion(label.CacheRegion);
            Logger.LogInformation($"MergeLabelsIntoLabel `{label}`, Merged Label Ids [{ string.Join(",", labelIdsToMerge) }] By User `{user}`");

            sw.Stop();
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = !errors.Any(),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public Task<OperationResult<Library.Models.Image>> SetLabelImageByUrlAsync(Library.Models.Users.User user, Guid id, string imageUrl) => SaveImageBytes(user, id, WebHelper.BytesForImageUrl(imageUrl));

        public async Task<OperationResult<bool>> UpdateLabelAsync(Library.Models.Users.User user, Label model)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var label = await DbContext.Labels.FirstOrDefaultAsync(x => x.RoadieId == model.Id).ConfigureAwait(false);
            if (label == null)
            {
                return new OperationResult<bool>(true, $"Label Not Found [{model.Id}]");
            }
            // If label is being renamed, see if label already exists with new model supplied name
            var labelName = label.SortNameValue;
            var labelModelName = model.SortNameValue;
            var oldPathToImage = label.PathToImage(Configuration);
            var didChangeName = !labelName.ToAlphanumericName().Equals(labelModelName.ToAlphanumericName(), StringComparison.OrdinalIgnoreCase);
            if (didChangeName)
            {
                var existingLabel = DbContext.Labels.FirstOrDefault(x => x.Name == model.Name || x.SortName == model.SortName);
                if (existingLabel != null)
                {
                    return new OperationResult<bool>($"Label already exists `{ existingLabel }` with name [{ labelModelName }].");
                }
            }
            try
            {
                var now = DateTime.UtcNow;
                var specialLabelName = model.Name.ToAlphanumericName();
                var alt = new List<string>(model.AlternateNamesList);
                if (!model.AlternateNamesList.Contains(specialLabelName, StringComparer.OrdinalIgnoreCase))
                {
                    alt.Add(specialLabelName);
                }
                label.AlternateNames = alt.ToDelimitedList();
                label.BeginDate = model.BeginDate;
                label.DiscogsId = model.DiscogsId;
                label.EndDate = model.EndDate;
                label.IsLocked = model.IsLocked;
                label.MusicBrainzId = model.MusicBrainzId;
                label.Name = model.Name;
                label.Profile = model.Profile;
                label.SortName = model.SortName;
                label.Status = SafeParser.ToEnum<Statuses>(model.Status);
                label.Tags = model.TagsList.ToDelimitedList();
                label.URLs = model.URLsList.ToDelimitedList();

                if (didChangeName)
                {
                    if (File.Exists(oldPathToImage))
                    {
                        File.Move(oldPathToImage, label.PathToImage(Configuration));
                    }
                }
                var labelImage = ImageHelper.ImageDataFromUrl(model.NewThumbnailData);
                if (labelImage != null)
                {
                    // Save unaltered label image
                    File.WriteAllBytes(label.PathToImage(Configuration, true), ImageHelper.ConvertToJpegFormat(labelImage));
                }
                label.LastUpdated = now;
                await DbContext.SaveChangesAsync().ConfigureAwait(false);

                CacheManager.ClearRegion(label.CacheRegion);
                Logger.LogInformation($"UpdateLabel `{label}` By User `{user}`");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                errors.Add(ex);
            }

            sw.Stop();

            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = !errors.Any(),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<Library.Models.Image>> UploadLabelImageAsync(Library.Models.Users.User user, Guid id, IFormFile file)
        {
            var bytes = new byte[0];
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                bytes = ms.ToArray();
            }
            return await SaveImageBytes(user, id, bytes).ConfigureAwait(false);
        }
    }
}