using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Imaging;
using Roadie.Library.Models.Collections;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Playlists;
using Roadie.Library.Models.Releases;
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
            IRoadieDbContext context,
            ICacheManager cacheManager,
            ILogger<BookmarkService> logger)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
        }

        public async Task<Library.Models.Pagination.PagedResult<models.BookmarkList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false, BookmarkType? filterType = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            var result = from b in DbContext.Bookmarks
                         join u in DbContext.Users on b.UserId equals u.Id
                         where b.UserId == roadieUser.Id
                         where filterType == null || b.BookmarkType == filterType
                         select new models.BookmarkList
                         {
                             Comment = b.Comment,
                             Position = b.Position,
                             User = new models.DataToken
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
                         };

            var sortBy = string.IsNullOrEmpty(request.Sort)
                ? request.OrderValue(new Dictionary<string, string> { { "CreatedDate", "DESC" } })
                : request.OrderValue();
            var rowCount = result.Count();
            var rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();

            var user = await GetUser(roadieUser.UserId);

            foreach (var row in rows)
                switch (row.Type)
                {
                    case BookmarkType.Artist:
                        var artist = DbContext.Artists.FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        if (artist == null) continue;
                        row.Bookmark = new models.DataToken
                        {
                            Text = artist.Name,
                            Value = artist.RoadieId.ToString()
                        };
                        row.Artist =
                            models.ArtistList.FromDataArtist(artist, ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, artist.RoadieId));
                        row.Thumbnail = ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, artist.RoadieId);
                        row.SortName = artist.SortName ?? artist.Name;
                        break;

                    case BookmarkType.Release:
                        var release = DbContext.Releases.Include(x => x.Artist)
                            .FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        if (release == null) continue;
                        row.Bookmark = new models.DataToken
                        {
                            Text = release.Title,
                            Value = release.RoadieId.ToString()
                        };
                        row.Release = ReleaseList.FromDataRelease(release, release.Artist, HttpContext.BaseUrl,
                            ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, release.Artist.RoadieId),
                            ImageHelper.MakeReleaseThumbnailImage(Configuration, HttpContext, release.RoadieId));
                        row.Thumbnail = ImageHelper.MakeReleaseThumbnailImage(Configuration, HttpContext, release.RoadieId);
                        row.SortName = release.SortTitleValue;
                        break;

                    case BookmarkType.Track:
                        var track = DbContext.Tracks
                            .Include(x => x.ReleaseMedia)
                            .Include(x => x.ReleaseMedia.Release)
                            .Include(x => x.ReleaseMedia.Release.Artist)
                            .Include(x => x.TrackArtist)
                            .FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        if (track == null) continue;
                        row.Bookmark = new models.DataToken
                        {
                            Text = track.Title,
                            Value = track.RoadieId.ToString()
                        };
                        row.Track = models.TrackList.FromDataTrack(MakeTrackPlayUrl(user, HttpContext.BaseUrl, track.RoadieId),
                            track,
                            track.ReleaseMedia.MediaNumber,
                            track.ReleaseMedia.Release,
                            track.ReleaseMedia.Release.Artist,
                            track.TrackArtist,
                            HttpContext.BaseUrl,
                            ImageHelper.MakeTrackThumbnailImage(Configuration, HttpContext, track.RoadieId),
                            ImageHelper.MakeReleaseThumbnailImage(Configuration, HttpContext, track.ReleaseMedia.Release.RoadieId),
                            ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, track.ReleaseMedia.Release.Artist.RoadieId),
                            ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, track.TrackArtist == null
                                ? null
                                : (Guid?)track.TrackArtist.RoadieId));
                        row.Track.TrackPlayUrl = MakeTrackPlayUrl(user, HttpContext.BaseUrl, track.RoadieId);
                        row.Thumbnail = ImageHelper.MakeTrackThumbnailImage(Configuration, HttpContext, track.RoadieId);
                        row.SortName = track.Title;
                        break;

                    case BookmarkType.Playlist:
                        var playlist = DbContext.Playlists
                            .Include(x => x.User)
                            .FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        if (playlist == null) continue;
                        row.Bookmark = new models.DataToken
                        {
                            Text = playlist.Name,
                            Value = playlist.RoadieId.ToString()
                        };
                        row.Playlist = PlaylistList.FromDataPlaylist(playlist, playlist.User,
                            ImageHelper.MakePlaylistThumbnailImage(Configuration, HttpContext, playlist.RoadieId),
                            ImageHelper.MakeUserThumbnailImage(Configuration, HttpContext, playlist.User.RoadieId));
                        row.Thumbnail = ImageHelper.MakePlaylistThumbnailImage(Configuration, HttpContext, playlist.RoadieId);
                        row.SortName = playlist.Name;
                        break;

                    case BookmarkType.Collection:
                        var collection = DbContext.Collections.FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        if (collection == null) continue;
                        row.Bookmark = new models.DataToken
                        {
                            Text = collection.Name,
                            Value = collection.RoadieId.ToString()
                        };
                        row.Collection = CollectionList.FromDataCollection(collection,
                            (from crc in DbContext.CollectionReleases
                             where crc.CollectionId == collection.Id
                             select crc.Id).Count(), ImageHelper.MakeCollectionThumbnailImage(Configuration, HttpContext, collection.RoadieId));
                        row.Thumbnail = ImageHelper.MakeCollectionThumbnailImage(Configuration, HttpContext, collection.RoadieId);
                        row.SortName = collection.SortName ?? collection.Name;
                        break;

                    case BookmarkType.Label:
                        var label = DbContext.Labels.FirstOrDefault(x => x.Id == row.BookmarkTargetId);
                        if (label == null) continue;
                        row.Bookmark = new models.DataToken
                        {
                            Text = label.Name,
                            Value = label.RoadieId.ToString()
                        };
                        row.Label = models.LabelList.FromDataLabel(label, ImageHelper.MakeLabelThumbnailImage(Configuration, HttpContext, label.RoadieId));
                        row.Thumbnail = ImageHelper.MakeLabelThumbnailImage(Configuration, HttpContext, label.RoadieId);
                        row.SortName = label.SortName ?? label.Name;
                        break;
                }

            ;
            sw.Stop();
            return new Library.Models.Pagination.PagedResult<models.BookmarkList>
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