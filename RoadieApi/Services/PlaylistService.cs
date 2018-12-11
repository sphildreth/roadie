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

namespace Roadie.Api.Services
{
    public class PlaylistService : ServiceBase, IPlaylistService
    {
        private IBookmarkService BookmarkService { get; } = null;

        public PlaylistService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext dbContext,
                             ICacheManager cacheManager,
                             ILogger<PlaylistService> logger,
                             IBookmarkService bookmarkService)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
            this.BookmarkService = bookmarkService;
        }

        public async Task<OperationResult<Playlist>> ById(User roadieUser, Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = string.Format("urn:playlist_by_id_operation:{0}:{1}", id, includes == null ? "0" : string.Join("|", includes));
            var result = await this.CacheManager.GetAsync<OperationResult<Playlist>>(cacheKey, async () =>
            {
                return await this.PlaylistByIdAction(id, includes);
            }, data.Artist.CacheRegionUrn(id));
            sw.Stop();
            if (result?.Data != null && roadieUser != null)
            {
                result.Data.UserCanEdit = result.Data.Maintainer.Id == roadieUser.UserId || roadieUser.IsAdmin;
                var userBookmarkResult = await this.BookmarkService.List(roadieUser, new PagedRequest(), false, BookmarkType.Playlist);
                if (userBookmarkResult.IsSuccess)
                {
                    result.Data.UserBookmarked = userBookmarkResult?.Rows?.FirstOrDefault(x => x.Bookmark.Text == result.Data.Id.ToString()) != null;
                }
            }
            return new OperationResult<Playlist>(result.Messages)
            {
                Data = result?.Data,
                IsNotFoundResult = result?.IsNotFoundResult ?? false,
                Errors = result?.Errors,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        private async Task<OperationResult<Playlist>> PlaylistByIdAction(Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();

            var playlist = this.GetPlaylist(id);

            if (playlist == null)
            {
                return new OperationResult<Playlist>(true, string.Format("Playlist Not Found [{0}]", id));
            }

            var result = playlist.Adapt<Playlist>();
            result.AlternateNames = playlist.AlternateNames;
            result.Tags = playlist.Tags;
            result.URLs = playlist.URLs;
            var maintainer = this.DbContext.Users.Include(x => x.UserRoles).Include("UserRoles.Role").FirstOrDefault(x => x.Id == playlist.UserId);
            result.Maintainer = UserList.FromDataUser(maintainer, this.MakeUserThumbnailImage(maintainer.RoadieId));
            result.Thumbnail = this.MakePlaylistThumbnailImage(playlist.RoadieId);
            result.MediumThumbnail = base.MakeThumbnailImage(id, "playlist", this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height);
            if (includes != null && includes.Any())
            {
                var playlistTracks = (from pl in this.DbContext.Playlists
                                      join pltr in this.DbContext.PlaylistTracks on pl.Id equals pltr.PlayListId
                                      join t in this.DbContext.Tracks on pltr.TrackId equals t.Id
                                      where pl.Id == playlist.Id
                                      select new { t, pltr });

                if (includes.Contains("stats"))
                {
                    result.Statistics = new Library.Models.Statistics.ReleaseGroupingStatistics
                    {
                        ReleaseCount = result.ReleaseCount,
                        TrackCount = result.TrackCount,
                        TrackSize = result.DurationTime,
                        FileSize = playlistTracks.Sum(x => (long?)x.t.FileSize).ToFileSize()
                    };
                }
                if (includes.Contains("tracks"))
                {
                    result.Tracks = (from plt in playlistTracks
                                      join rm in this.DbContext.ReleaseMedias on plt.t.ReleaseMediaId equals rm.Id
                                      join r in this.DbContext.Releases on rm.ReleaseId equals r.Id
                                      join releaseArtist in this.DbContext.Artists on r.ArtistId equals releaseArtist.Id
                                      join trackArtist in this.DbContext.Artists on plt.t.ArtistId equals trackArtist.Id into tas
                                      from trackArtist in tas.DefaultIfEmpty()
                                      select new PlaylistTrack
                                      {
                                          ListNumber = plt.pltr.ListNumber,
                                          Track = TrackList.FromDataTrack(plt.t,
                                                                          rm.MediaNumber,
                                                                          r,
                                                                          releaseArtist,
                                                                          trackArtist,
                                                                          this.HttpContext.BaseUrl,
                                                                          this.MakeTrackThumbnailImage(plt.t.RoadieId),
                                                                          this.MakeReleaseThumbnailImage(r.RoadieId),
                                                                          this.MakeArtistThumbnailImage(releaseArtist.RoadieId),
                                                                          this.MakeArtistThumbnailImage(trackArtist == null ? null : (Guid?)trackArtist.RoadieId))
                                      }).ToArray();
                }

            }

            sw.Stop();
            return new OperationResult<Playlist>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            };

        }


        public async Task<Library.Models.Pagination.PagedResult<PlaylistList>> List(PagedRequest request, User roadieUser = null)
        {
            var sw = new Stopwatch();
            sw.Start();

            int[] playlistWithArtistTrackIds = new int[0];
            if(request.FilterToArtistId.HasValue)
            {
                playlistWithArtistTrackIds = (from pl in this.DbContext.Playlists
                                          join pltr in this.DbContext.PlaylistTracks on pl.Id equals pltr.PlayListId
                                          join t in this.DbContext.Tracks on pltr.TrackId equals t.Id
                                          join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                          join r in this.DbContext.Releases on rm.ReleaseId equals r.Id
                                          join a in this.DbContext.Artists on r.ArtistId equals a.Id
                                          where a.RoadieId == request.FilterToArtistId
                                          select pl.Id
                                         ).ToArray();
            }
            int[] playlistReleaseTrackIds = new int[0];
            if(request.FilterToReleaseId.HasValue)
            {
                playlistReleaseTrackIds = (from pl in this.DbContext.Playlists
                                              join pltr in this.DbContext.PlaylistTracks on pl.Id equals pltr.PlayListId
                                              join t in this.DbContext.Tracks on pltr.TrackId equals t.Id
                                              join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                              join r in this.DbContext.Releases on rm.ReleaseId equals r.Id
                                              where r.RoadieId == request.FilterToReleaseId
                                              select pl.Id
                                         ).ToArray();
            }

            var result = (from pl in this.DbContext.Playlists
                          join u in this.DbContext.Users on pl.UserId equals u.Id
                          where (request.FilterToPlaylistId == null || pl.RoadieId == request.FilterToPlaylistId)
                          where (request.FilterToArtistId == null || playlistWithArtistTrackIds.Contains(pl.Id))
                          where (request.FilterToReleaseId == null || playlistReleaseTrackIds.Contains(pl.Id))
                          where ((roadieUser == null && pl.IsPublic) || (roadieUser != null && u.RoadieId == roadieUser.UserId || pl.IsPublic))
                          where (request.FilterValue.Length == 0 || (request.FilterValue.Length > 0 && (pl.Name != null && pl.Name.Contains(request.FilterValue))))
                          select PlaylistList.FromDataPlaylist(pl, u, this.MakePlaylistThumbnailImage(pl.RoadieId), this.MakeUserThumbnailImage(u.RoadieId)));
            var sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "Playlist.Text", "ASC" } }) : request.OrderValue(null);
            var rowCount = result.Count();
            var rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
            sw.Stop();
            return new Library.Models.Pagination.PagedResult<PlaylistList>
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