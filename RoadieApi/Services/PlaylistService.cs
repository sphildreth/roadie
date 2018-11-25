using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Playlists;
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
        public PlaylistService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext dbContext,
                             ICacheManager cacheManager,
                             ILogger<PlaylistService> logger)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
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

            var result = (from pl in this.DbContext.Playlists
                          join u in this.DbContext.Users on pl.UserId equals u.Id
                          let duration = (from plt in this.DbContext.PlaylistTracks 
                                          join t in this.DbContext.Tracks on plt.TrackId equals t.Id
                                          where plt.PlayListId == pl.Id
                                          select t.Duration).Sum()
                          where (request.FilterToPlaylistId == null || pl.RoadieId == request.FilterToPlaylistId)
                          where (request.FilterToArtistId == null || playlistWithArtistTrackIds.Contains(pl.Id))
                          where ((roadieUser == null && pl.IsPublic) || (roadieUser != null && u.RoadieId == roadieUser.UserId || pl.IsPublic))
                          where (request.FilterValue.Length == 0 || (request.FilterValue.Length > 0 && (
                                    pl.Name != null && pl.Name.Contains(request.FilterValue))
                          ))
                          select new PlaylistList
                          {
                              Playlist = new DataToken
                              {
                                  Text = pl.Name,
                                  Value = pl.RoadieId.ToString()
                                  
                              },
                              User = new DataToken
                              {
                                  Text = u.UserName,
                                  Value = u.RoadieId.ToString()
                              },
                              PlaylistCount = this.DbContext.PlaylistTracks.Where(x => x.PlayListId == pl.Id).Count(),
                              IsPublic = pl.IsPublic,
                              Duration = duration,
                              CreatedDate = pl.CreatedDate,
                              LastUpdated = pl.LastUpdated,
                              UserThumbnail = MakeUserThumbnailImage(u.RoadieId),
                              Id = pl.RoadieId,
                              Thumbnail = MakePlaylistThumbnailImage(pl.RoadieId)
                          });
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