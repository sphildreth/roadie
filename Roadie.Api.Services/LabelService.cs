using Mapster;
using Microsoft.AspNetCore.Http;
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
using Roadie.Library.Models.Pagination;
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
        private IBookmarkService BookmarkService { get; } = null;

        public LabelService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext context,
                             ICacheManager cacheManager,
                             ILogger<LabelService> logger,
                             ICollectionService collectionService,
                             IPlaylistService playlistService,
                             IBookmarkService bookmarkService)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
            this.BookmarkService = bookmarkService;
        }

        public async Task<OperationResult<Label>> ById(User roadieUser, Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = string.Format("urn:label_by_id_operation:{0}:{1}", id, includes == null ? "0" : string.Join("|", includes));
            var result = await this.CacheManager.GetAsync<OperationResult<Label>>(cacheKey, async () =>
            {
                return await this.LabelByIdAction(id, includes);
            }, data.Artist.CacheRegionUrn(id));
            sw.Stop();
            if (result?.Data != null && roadieUser != null)
            {
                var userBookmarkResult = await this.BookmarkService.List(roadieUser, new PagedRequest(), false, BookmarkType.Label);
                if (userBookmarkResult.IsSuccess)
                {
                    result.Data.UserBookmarked = userBookmarkResult?.Rows?.FirstOrDefault(x => x.Bookmark.Text == result.Data.Id.ToString()) != null;
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

        public Task<Library.Models.Pagination.PagedResult<LabelList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false)
        {
            var sw = new Stopwatch();
            sw.Start();

            if (!string.IsNullOrEmpty(request.Sort))
            {
                request.Sort = request.Sort.Replace("createdDate", "createdDateTime");
                request.Sort = request.Sort.Replace("lastUpdated", "lastUpdatedDateTime");
            }
            var normalizedFilterValue = !string.IsNullOrEmpty(request.FilterValue) ? request.FilterValue.ToAlphanumericName() : null;
            var result = (from l in this.DbContext.Labels
                          where (request.FilterValue.Length == 0 || (request.FilterValue.Length > 0 && (
                                    l.Name != null && l.Name.Contains(request.FilterValue) ||
                                    l.AlternateNames != null && l.AlternateNames.Contains(request.FilterValue) ||
                                    l.AlternateNames != null && l.AlternateNames.Contains(normalizedFilterValue)
                          )))
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
                              Thumbnail = this.MakeLabelThumbnailImage(l.RoadieId)
                          });
            LabelList[] rows = null;
            var rowCount = result.Count();
            if (doRandomize ?? false)
            {
                var randomLimit = roadieUser?.RandomReleaseLimit ?? 100;
                request.Limit = request.LimitValue > randomLimit ? randomLimit : request.LimitValue;
                var sql = "SELECT l.Id FROM `label` l ORDER BY RAND() LIMIT {0}";
                rows = (from rdn in this.DbContext.Labels.FromSql(sql, randomLimit)
                        join rs in result on rdn.Id equals rs.DatabaseId
                        select rs)
                        .Take(request.LimitValue)
                        .ToArray();
            }
            else
            {
                var sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "SortName", "ASC" }, { "Label.Text", "ASC" } }) : request.OrderValue(null);
                rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
            }
            sw.Stop();
            return Task.FromResult(new Library.Models.Pagination.PagedResult<LabelList>
            {
                TotalCount = rowCount,
                CurrentPage = request.PageValue,
                TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                OperationTime = sw.ElapsedMilliseconds,
                Rows = rows
            });
        }

        public async Task<OperationResult<Image>> SetLabelImageByUrl(User user, Guid id, string imageUrl)
        {
            return await this.SaveImageBytes(user, id, WebHelper.BytesForImageUrl(imageUrl));
        }

        public async Task<OperationResult<bool>> UpdateLabel(User user, Label model)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var label = this.DbContext.Labels.FirstOrDefault(x => x.RoadieId == model.Id);
            if (label == null)
            {
                return new OperationResult<bool>(true, string.Format("Label Not Found [{0}]", model.Id));
            }
            try
            {
                var now = DateTime.UtcNow;
                label.AlternateNames = model.AlternateNamesList.ToDelimitedList();
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

                var labelImage = ImageHelper.ImageDataFromUrl(model.NewThumbnailData);
                if (labelImage != null)
                {
                    // Ensure is jpeg first
                    label.Thumbnail = ImageHelper.ConvertToJpegFormat(labelImage);

                    // Resize to store in database as thumbnail
                    label.Thumbnail = ImageHelper.ResizeImage(label.Thumbnail, this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height);
                }
                label.LastUpdated = now;
                await this.DbContext.SaveChangesAsync();

                this.CacheManager.ClearRegion(label.CacheRegion);
                this.Logger.LogInformation($"UpdateLabel `{ label }` By User `{ user }`");
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex);
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

        public async Task<OperationResult<Image>> UploadLabelImage(User user, Guid id, IFormFile file)
        {
            var bytes = new byte[0];
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                bytes = ms.ToArray();
            }
            return await this.SaveImageBytes(user, id, bytes);
        }

        private Task<OperationResult<Label>> LabelByIdAction(Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();

            var label = this.GetLabel(id);

            if (label == null)
            {
                return Task.FromResult(new OperationResult<Label>(true, string.Format("Label Not Found [{0}]", id)));
            }

            var result = label.Adapt<Label>();
            result.AlternateNames = label.AlternateNames;
            result.Tags = label.Tags;
            result.URLs = label.URLs;
            result.Thumbnail = this.MakeLabelThumbnailImage(label.RoadieId);
            result.MediumThumbnail = base.MakeThumbnailImage(id, "label", this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height);
            if (includes != null && includes.Any())
            {
                if (includes.Contains("stats"))
                {
                    var labelTracks = (from l in this.DbContext.Labels
                                       join rl in this.DbContext.ReleaseLabels on l.Id equals rl.LabelId into rld
                                       from rl in rld.DefaultIfEmpty()
                                       join r in this.DbContext.Releases on rl.ReleaseId equals r.Id
                                       join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                       join t in this.DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                       where (l.Id == label.Id)
                                       select new
                                       {
                                           t.Duration,
                                           t.FileSize
                                       });
                    result.Duration = labelTracks.Sum(x => x.Duration);

                    result.Statistics = new Library.Models.Statistics.ReleaseGroupingStatistics
                    {
                        TrackCount = label.TrackCount,
                        ArtistCount = label.ArtistCount,
                        ReleaseCount = label.ReleaseCount,
                        TrackSize = result.DurationTime,
                        FileSize = labelTracks.Sum(x => (long?)x.FileSize).ToFileSize()
                    };
                }
            }

            sw.Stop();
            return Task.FromResult(new OperationResult<Label>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        private async Task<OperationResult<Library.Models.Image>> SaveImageBytes(User user, Guid id, byte[] imageBytes)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var label = this.DbContext.Labels.FirstOrDefault(x => x.RoadieId == id);
            if (label == null)
            {
                return new OperationResult<Library.Models.Image>(true, string.Format("Label Not Found [{0}]", id));
            }
            try
            {
                var now = DateTime.UtcNow;
                label.Thumbnail = imageBytes;
                if (label.Thumbnail != null)
                {
                    // Ensure is jpeg first
                    label.Thumbnail = ImageHelper.ConvertToJpegFormat(label.Thumbnail);

                    // Resize to store in database as thumbnail
                    label.Thumbnail = ImageHelper.ResizeImage(label.Thumbnail, this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height);
                }
                label.LastUpdated = now;
                await this.DbContext.SaveChangesAsync();
                this.CacheManager.ClearRegion(label.CacheRegion);
                this.Logger.LogInformation($"UploadLabelImage `{ label }` By User `{ user }`");
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex);
                errors.Add(ex);
            }
            sw.Stop();

            return new OperationResult<Library.Models.Image>
            {
                IsSuccess = !errors.Any(),
                Data = base.MakeThumbnailImage(id, "label", this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height, true),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }
    }
}