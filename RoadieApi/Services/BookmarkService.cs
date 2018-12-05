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
                             ILogger<BookmarkService> logger,
                             ICollectionService collectionService,
                             IPlaylistService playlistService)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
        }

        public async Task<Library.Models.Pagination.PagedResult<BookmarkList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false, BookmarkType? filterType = null)
        {
            
            var sw = new Stopwatch();
            sw.Start();
            var result = (from b in this.DbContext.Bookmarks
                          join u in this.DbContext.Users on b.UserId equals u.Id
                          where b.UserId == roadieUser.Id
                          where (filterType == null || b.BookmarkType == filterType)
                          select new BookmarkList
                          {
                              Comment = b.Comment,
                              Position = b.Position,
                              User = new DataToken
                              {
                                  Text = u.UserName,
                                  Value = u.RoadieId.ToString()
                              },                               
                              DatabaseId = b.Id,
                              Id = b.RoadieId,
                              CreatedDate = b.CreatedDate,
                              LastUpdated = b.LastUpdated,
                              Type = b.BookmarkType,
                              BookmarkTargetId = b.BookmarkTargetId                               
                          });

            var sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "CreatedDate", "DESC" } }) : request.OrderValue(null);
            var rowCount = result.Count();
            BookmarkList[] rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();

            foreach (var row in rows)
            {
                switch (row.Type)
                {
                    case BookmarkType.Artist:
                        var artist = this.DbContext.Artists.FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        row.Bookmark = new DataToken
                        {
                            Text = artist.Name,
                            Value = artist.RoadieId.ToString()
                        };
                        row.Thumbnail = this.MakeArtistThumbnailImage(artist.RoadieId);
                        row.SortName = artist.SortName ?? artist.Name;
                        break;

                    case BookmarkType.Release:
                        var release = this.DbContext.Releases.FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        row.Bookmark = new DataToken
                        {
                            Text = release.Title,
                            Value = release.RoadieId.ToString()
                        };
                        row.Thumbnail = this.MakeReleaseThumbnailImage(release.RoadieId);
                        row.SortName = release.Title;
                        break;

                    case BookmarkType.Track:
                        var track = this.DbContext.Tracks.FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        row.Bookmark = new DataToken
                        {
                            Text = track.Title,
                            Value = track.RoadieId.ToString()
                        };
                        row.Thumbnail = this.MakeTrackThumbnailImage(track.RoadieId);
                        row.SortName = track.Title;
                        break;

                    case BookmarkType.Playlist:
                        var playlist = this.DbContext.Playlists.FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        row.Bookmark = new DataToken
                        {
                            Text = playlist.Name,
                            Value = playlist.RoadieId.ToString()
                        };
                        row.Thumbnail = this.MakePlaylistThumbnailImage(playlist.RoadieId);
                        row.SortName = playlist.Name;
                        break;

                    case BookmarkType.Collection:
                        var collection = this.DbContext.Collections.FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        row.Bookmark = new DataToken
                        {
                            Text = collection.Name,
                            Value = collection.RoadieId.ToString()
                        };
                        row.Thumbnail = this.MakeCollectionThumbnailImage(collection.RoadieId);
                        row.SortName = collection.SortName ?? collection.Name;
                        break;

                    case BookmarkType.Label:
                        var label = this.DbContext.Labels.FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        row.Bookmark = new DataToken
                        {
                            Text = label.Name,
                            Value = label.RoadieId.ToString()
                        };
                        row.Thumbnail = this.MakeLabelThumbnailImage(label.RoadieId);
                        row.SortName = label.SortName ?? label.Name;
                        break;
                }
            };
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