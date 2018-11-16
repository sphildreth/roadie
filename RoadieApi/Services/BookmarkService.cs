using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
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
    public class BookmarkService : ServiceBase, IBookmarkService
    {
        public BookmarkService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext context,
                             ICacheManager cacheManager,
                             ILogger<ArtistService> logger,
                             ICollectionService collectionService,
                             IPlaylistService playlistService)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
        }

        public async Task<Library.Models.Pagination.PagedResult<BookmarkList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false, BookmarkType? filterType = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            if (!string.IsNullOrEmpty(request.Sort))
            {
                request.Sort = request.Sort.Replace("bookmarkType", "bookmarkTypeValue");
                request.Sort = request.Sort.Replace("createdDate", "createdDateTime");
                request.Sort = request.Sort.Replace("lastUpdated", "lastUpdatedDateTime");
            }

            var result = (from b in this.DbContext.Bookmarks
                          where b.UserId == roadieUser.Id
                          where (filterType == null || b.BookmarkType == filterType)
                          select new BookmarkList
                          {
                              DatabaseId = b.Id,
                              Id = b.RoadieId,
                              CreatedDate = b.CreatedDate,
                              LastUpdated = b.LastUpdated,
                              Type = b.BookmarkType,
                              BookmarkTargetId = b.BookmarkTargetId
                          });

            var sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "CreatedDate", "DESC" } }) : request.OrderValue(null);
            var rowCount = result.Count();
            BookmarkList[] rows = rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();

            var datas = (from b in rows
                         join a in this.DbContext.Artists on b.BookmarkTargetId equals a.Id into aa
                         from a in aa.DefaultIfEmpty()
                         join r in this.DbContext.Releases on b.BookmarkTargetId equals r.Id into rr
                         from r in rr.DefaultIfEmpty()
                         join t in this.DbContext.Tracks on b.BookmarkTargetId equals t.Id into tt
                         from t in rr.DefaultIfEmpty()
                         join p in this.DbContext.Playlists on b.BookmarkTargetId equals p.Id into pp
                         from p in pp.DefaultIfEmpty()
                         join c in this.DbContext.Collections on b.BookmarkTargetId equals c.Id into cc
                         from c in cc.DefaultIfEmpty()
                         join l in this.DbContext.Labels on b.BookmarkTargetId equals l.Id into ll
                         from l in ll.DefaultIfEmpty()
                         select new
                         { b, a, r, t, p, c, l });

            foreach (var row in rows)
            {
                var d = datas.FirstOrDefault(x => x.b.DatabaseId == row.DatabaseId);
                switch (row.Type)
                {
                    case BookmarkType.Artist:
                        row.Bookmark = new DataToken
                        {
                            Text = d.a.Name,
                            Value = d.a.RoadieId.ToString()
                        };
                        row.Thumbnail = this.MakeArtistThumbnailImage(d.a.RoadieId);
                        break;

                    case BookmarkType.Release:
                        row.Bookmark = new DataToken
                        {
                            Text = d.r.Title,
                            Value = d.r.RoadieId.ToString()
                        };
                        row.Thumbnail = this.MakeReleaseThumbnailImage(d.r.RoadieId);
                        break;

                    case BookmarkType.Track:
                        row.Bookmark = new DataToken
                        {
                            Text = d.t.Title,
                            Value = d.t.RoadieId.ToString()
                        };
                        row.Thumbnail = this.MakeTrackThumbnailImage(d.t.RoadieId);
                        break;

                    case BookmarkType.Playlist:
                        row.Bookmark = new DataToken
                        {
                            Text = d.p.Name,
                            Value = d.p.RoadieId.ToString()
                        };
                        row.Thumbnail = this.MakePlaylistThumbnailImage(d.p.RoadieId);
                        break;

                    case BookmarkType.Collection:
                        row.Bookmark = new DataToken
                        {
                            Text = d.c.Name,
                            Value = d.c.RoadieId.ToString()
                        };
                        row.Thumbnail = this.MakeCollectionThumbnailImage(d.c.RoadieId);
                        break;

                    case BookmarkType.Label:
                        row.Bookmark = new DataToken
                        {
                            Text = d.l.Name,
                            Value = d.l.RoadieId.ToString()
                        };
                        row.Thumbnail = this.MakeLabelThumbnailImage(d.l.RoadieId);
                        break;
                }
            };
            sw.Stop();

            sw.Stop();
            return new Library.Models.Pagination.PagedResult<BookmarkList>
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