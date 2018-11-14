using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Playlists;
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

        public async Task<Library.Models.Pagination.PagedResult<PlaylistList>> PlaylistList(PagedRequest request, Guid? userId = null, Guid? artistId = null)
        {
            var sw = new Stopwatch();
            sw.Start();

            if (!string.IsNullOrEmpty(request.Sort))
            {
                request.Sort = request.Sort.Replace("createdDate", "createdDateTime");
                request.Sort = request.Sort.Replace("lastUpdated", "lastUpdatedDateTime");
            }
            var result = (from pl in this.DbContext.Playlists
                          join pltr in this.DbContext.PlaylistTracks on pl.Id equals pltr.PlayListId
                          join t in this.DbContext.Tracks on pltr.TrackId equals t.Id
                          join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                          join r in this.DbContext.Releases on rm.ReleaseId equals r.Id
                          join a in this.DbContext.Artists on r.ArtistId equals a.Id
                          join u in this.DbContext.Users on pl.UserId equals u.Id
                          where ((userId == null && pl.IsPublic) || (userId != null && u.RoadieId == userId || pl.IsPublic))
                          where (artistId == null || (artistId != null && a.RoadieId == artistId))
                          where (request.FilterValue.Length == 0 || (request.FilterValue.Length > 0 && (
                                    pl.Name != null && pl.Name.ToLower().Contains(request.Filter.ToLower()))
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
                                  Text = u.Username,
                                  Value = u.RoadieId.ToString()
                              },
                              PlaylistCount = this.DbContext.PlaylistTracks.Where(x => x.PlayListId == pl.Id).Count(),
                              CreatedDate = pl.CreatedDate,
                              LastUpdated = pl.LastUpdated,
                              UserThumbnail = MakeUserThumbnailImage(u.RoadieId),
                              Id = pl.RoadieId,
                              Thumbnail = MakePlaylistThumbnailImage(pl.RoadieId)
                          }).Distinct();
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