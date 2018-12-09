using Mapster;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Models;
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

        private async Task<OperationResult<Label>> LabelByIdAction(Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();

            var label = this.GetLabel(id);

            if (label == null)
            {
                return new OperationResult<Label>(true, string.Format("Label Not Found [{0}]", id));
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
            return new OperationResult<Label>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            };

        }


        public async Task<Library.Models.Pagination.PagedResult<LabelList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false)
        {
            var sw = new Stopwatch();
            sw.Start();

            if (!string.IsNullOrEmpty(request.Sort))
            {
                request.Sort = request.Sort.Replace("createdDate", "createdDateTime");
                request.Sort = request.Sort.Replace("lastUpdated", "lastUpdatedDateTime");
            }
            var result = (from l in this.DbContext.Labels
                          where (request.FilterValue.Length == 0 || (request.FilterValue.Length > 0 && (
                                    l.Name != null && l.Name.Contains(request.FilterValue) ||
                                    l.AlternateNames != null && l.AlternateNames.Contains(request.FilterValue)
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
            var sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "SortName", "ASC" }, { "Label.Text", "ASC" } }) : request.OrderValue(null);
            var rowCount = result.Count();
            var rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
            sw.Stop();
            return new Library.Models.Pagination.PagedResult<LabelList>
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