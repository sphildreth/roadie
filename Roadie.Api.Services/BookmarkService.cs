using Microsoft.EntityFrameworkCore;
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

using models = Roadie.Library.Models;

namespace Roadie.Api.Services
{
    public class BookmarkService : ServiceBase, IBookmarkService
    {
        public BookmarkService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext context,
                             ICacheManager cacheManager,
                             ILogger<BookmarkService> logger)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
        }

        public Task<Library.Models.Pagination.PagedResult<BookmarkList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false, BookmarkType? filterType = null)
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

            var user = this.GetUser(roadieUser.UserId);

            foreach (var row in rows)
            {
                switch (row.Type)
                {
                    case BookmarkType.Artist:
                        var artist = this.DbContext.Artists.FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        if(artist == null)
                        {
                            continue;
                        }
                        row.Bookmark = new DataToken
                        {
                            Text = artist.Name,
                            Value = artist.RoadieId.ToString()
                        };
                        row.Artist = models.ArtistList.FromDataArtist(artist, this.MakeArtistThumbnailImage(artist.RoadieId));
                        row.Thumbnail = this.MakeArtistThumbnailImage(artist.RoadieId);
                        row.SortName = artist.SortName ?? artist.Name;
                        break;

                    case BookmarkType.Release:
                        var release = this.DbContext.Releases.Include(x => x.Artist).FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        if (release == null)
                        {
                            continue;
                        }
                        row.Bookmark = new DataToken
                        {
                            Text = release.Title,
                            Value = release.RoadieId.ToString()
                        };
                        row.Release = models.Releases.ReleaseList.FromDataRelease(release, release.Artist, this.HttpContext.BaseUrl, this.MakeArtistThumbnailImage(release.Artist.RoadieId), this.MakeReleaseThumbnailImage(release.RoadieId));
                        row.Thumbnail = this.MakeReleaseThumbnailImage(release.RoadieId);
                        row.SortName = release.Title;
                        break;

                    case BookmarkType.Track:
                        var track = this.DbContext.Tracks
                                                  .Include(x => x.ReleaseMedia)
                                                  .Include(x => x.ReleaseMedia.Release)
                                                  .Include(x => x.ReleaseMedia.Release.Artist)
                                                  .Include(x => x.TrackArtist)
                                                  .FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        if (track == null)
                        {
                            continue;
                        }
                        row.Bookmark = new DataToken
                        {
                            Text = track.Title,
                            Value = track.RoadieId.ToString()
                        };
                        row.Track = TrackList.FromDataTrack(this.MakeTrackPlayUrl(user, track.Id, track.RoadieId),
                                                            track,
                                                            track.ReleaseMedia.MediaNumber,
                                                            track.ReleaseMedia.Release,
                                                            track.ReleaseMedia.Release.Artist,
                                                            track.TrackArtist,
                                                            this.HttpContext.BaseUrl,
                                                            this.MakeTrackThumbnailImage(track.RoadieId),
                                                            this.MakeReleaseThumbnailImage(track.ReleaseMedia.Release.RoadieId),
                                                            this.MakeArtistThumbnailImage(track.ReleaseMedia.Release.Artist.RoadieId),
                                                            this.MakeArtistThumbnailImage(track.TrackArtist == null ? null : (Guid?)track.TrackArtist.RoadieId));
                        row.Track.TrackPlayUrl = this.MakeTrackPlayUrl(user, track.Id, track.RoadieId);
                        row.Thumbnail = this.MakeTrackThumbnailImage(track.RoadieId);
                        row.SortName = track.Title;
                        break;

                    case BookmarkType.Playlist:
                        var playlist = this.DbContext.Playlists
                                                     .Include(x => x.User)
                                                     .FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        if (playlist == null)
                        {
                            continue;
                        }
                        row.Bookmark = new DataToken
                        {
                            Text = playlist.Name,
                            Value = playlist.RoadieId.ToString()
                        };
                        row.Playlist = models.Playlists.PlaylistList.FromDataPlaylist(playlist, playlist.User, this.MakePlaylistThumbnailImage(playlist.RoadieId), this.MakeUserThumbnailImage(playlist.User.RoadieId));
                        row.Thumbnail = this.MakePlaylistThumbnailImage(playlist.RoadieId);
                        row.SortName = playlist.Name;
                        break;

                    case BookmarkType.Collection:
                        var collection = this.DbContext.Collections.FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        if (collection == null)
                        {
                            continue;
                        }
                        row.Bookmark = new DataToken
                        {
                            Text = collection.Name,
                            Value = collection.RoadieId.ToString()
                        };
                        row.Collection = models.Collections.CollectionList.FromDataCollection(collection, (from crc in this.DbContext.CollectionReleases
                                                                                                           where crc.CollectionId == collection.Id
                                                                                                           select crc.Id).Count(), this.MakeCollectionThumbnailImage(collection.RoadieId));
                        row.Thumbnail = this.MakeCollectionThumbnailImage(collection.RoadieId);
                        row.SortName = collection.SortName ?? collection.Name;
                        break;

                    case BookmarkType.Label:
                        var label = this.DbContext.Labels.FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        if (label == null)
                        {
                            continue;
                        }
                        row.Bookmark = new DataToken
                        {
                            Text = label.Name,
                            Value = label.RoadieId.ToString()
                        };
                        row.Label = models.LabelList.FromDataLabel(label, this.MakeLabelThumbnailImage(label.RoadieId));
                        row.Thumbnail = this.MakeLabelThumbnailImage(label.RoadieId);
                        row.SortName = label.SortName ?? label.Name;
                        break;
                }
            };
            sw.Stop();
            return Task.FromResult(new Library.Models.Pagination.PagedResult<BookmarkList>
            {
                TotalCount = rowCount,
                CurrentPage = request.PageValue,
                TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                OperationTime = sw.ElapsedMilliseconds,
                Rows = rows
            });
        }
    }
}