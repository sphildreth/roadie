using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
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
    public class ReleaseService : ServiceBase, IReleaseService
    {
        public ReleaseService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext dbContext,
                             ICacheManager cacheManager,
                             ILogger<ReleaseService> logger)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
        }

        public async Task<Library.Models.Pagination.PagedResult<ReleaseList>> ReleaseList(User user, PagedRequest request, bool? doRandomize = false, IEnumerable<string> includes = null)
        {
            var sw = new Stopwatch();
            sw.Start();

            if (!string.IsNullOrEmpty(request.Sort))
            {
                request.Sort = request.Sort.Replace("createdDate", "createdDateTime");
                request.Sort = request.Sort.Replace("lastUpdated", "lastUpdatedDateTime");
                request.Sort = request.Sort.Replace("ReleaseDate", "ReleaseDateDateTime");
                request.Sort = request.Sort.Replace("releaseYear", "ReleaseDateDateTime");
            }

            var resultQuery = (from r in this.DbContext.Releases.Include("Artist")
                               join a in this.DbContext.Artists on r.ArtistId equals a.Id
                               join userR in this.DbContext.UserReleases on r.Id equals userR.ReleaseId into userRas
                               from userR in userRas.DefaultIfEmpty()
                               join u in this.DbContext.Users on userR.UserId equals u.Id into ug
                               from u in ug.DefaultIfEmpty()
                               where (u == null || u.Id == user.Id)
                               where (request.FilterMinimumRating == null || userR.Rating >= request.FilterMinimumRating.Value)
                               where (request.FilterToArtistId == null || r.Artist.RoadieId == request.FilterToArtistId)
                               select new { r, a, userR, u });

            if (!string.IsNullOrEmpty(request.Filtervalue))
            {
                if (!request.Filtervalue.StartsWith("#"))
                {
                    // Find any releases by filter
                    resultQuery = resultQuery.Where(x =>
                        x.r.Title != null && x.r.Title.ToLower().Contains(request.Filter.ToLower()) ||
                        x.r.AlternateNames != null && x.r.AlternateNames.ToLower().Contains(request.Filter.ToLower())
                    );
                }
                else if (request.Filtervalue.StartsWith("#"))
                {
                    // Find any releases by tags
                    var tagValue = request.Filtervalue.Replace("#", "");
                    resultQuery = resultQuery.Where(x => x.r.Tags != null && x.r.Tags.ToLower().Contains(tagValue));
                }
            }

            var result = resultQuery.Select(x =>
                          new ReleaseList
                          {
                              Id = x.r.RoadieId,
                              Artist = new DataToken
                              {
                                  Value = x.r.Artist.RoadieId.ToString(),
                                  Text = x.r.Artist.Name
                              },
                              ArtistThumbnail = this.MakeArtistThumbnailImage(x.r.Artist.RoadieId),
                              Rating = x.userR.Rating,
                              ReleasePlayUrl = $"{ this.HttpContext.BaseUrl }/play/release/{ x.r.RoadieId}",
                              UserRating = x.userR != null ? (short?)x.userR.Rating : null,
                              LibraryStatus = x.r.LibraryStatus,
                              ReleaseDateDateTime = x.r.ReleaseDate,
                              Release = new DataToken
                              {
                                  Text = x.r.Title,
                                  Value = x.r.RoadieId.ToString()
                              },
                              Status = x.r.Status,
                              TrackCount = x.r.TrackCount,
                              CreatedDate = x.r.CreatedDate,
                              LastUpdated = x.r.LastUpdated,
                              TrackPlayedCount = (from ut in this.DbContext.UserTracks
                                                  join t in this.DbContext.Tracks on ut.TrackId equals t.Id
                                                  join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                  join rl in this.DbContext.Releases on rm.ReleaseId equals rl.Id
                                                  where rl.Id == x.r.Id
                                                  select ut.PlayedCount ?? 0).Sum(),
                              Thumbnail = this.MakeReleaseThumbnailImage(x.r.RoadieId)
                          }).Distinct();

            ReleaseList[] rows = null;

            var rowCount = result.Count();

            if (doRandomize ?? false)
            {
                request.Limit = request.LimitValue > user.RandomReleaseLimit ? user.RandomReleaseLimit : request.LimitValue;
                rows = result.OrderBy(x => Guid.NewGuid()).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
            }
            else
            {
                string sortBy = null;
                if (request.ActionValue == User.ActionKeyUserRated)
                {
                    sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "Rating", "DESC" } }) : request.OrderValue(null);
                }
                else
                {
                    sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "Release.Text", "ASC" } }) : request.OrderValue(null);
                }
                rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
            }

            if (includes != null && includes.Any())
            {
                if (includes.Contains("tracks"))
                {
                    var releaseIds = rows.Select(x => x.Id).ToArray();
                    var artistTracks = (from r in this.DbContext.Releases
                                        join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                        join t in this.DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                        join a in this.DbContext.Artists on r.ArtistId equals a.Id
                                        where (releaseIds.Contains(r.RoadieId))
                                        orderby r.Id, rm.MediaNumber, t.TrackNumber
                                        select new
                                        {
                                            t,
                                            releaseMedia = rm
                                        });
                    var releaseTrackIds = artistTracks.Select(x => x.t.Id).ToList();
                    var artistUserTracks = (from ut in this.DbContext.UserTracks
                                            where ut.UserId == user.Id
                                            where (from x in releaseTrackIds select x).Contains(ut.TrackId)
                                            select ut).ToArray();
                    foreach (var release in rows)
                    {
                        var releaseMedias = new List<ReleaseMediaList>();
                        foreach (var releaseMedia in artistTracks.Where(x => x.releaseMedia.RoadieId == release.Id).Select(x => x.releaseMedia).Distinct().ToArray())
                        {
                            var rm = releaseMedia.Adapt<ReleaseMediaList>();
                            var rmTracks = new List<TrackList>();
                            foreach (var track in artistTracks.Where(x => x.t.ReleaseMediaId == releaseMedia.Id).OrderBy(x => x.t.TrackNumber).ToArray())
                            {
                                var userRating = artistUserTracks.FirstOrDefault(x => x.TrackId == track.t.Id);
                                var t = track.t.Adapt<TrackList>();
                                t.CssClass = string.IsNullOrEmpty(track.t.Hash) ? "Missing" : "Ok";
                                t.TrackPlayUrl = $"{ this.HttpContext.BaseUrl }/play/track/{ track.t.RoadieId}";
                                t.UserRating = userRating == null ? null : (short?)userRating.Rating;
                                rmTracks.Add(t);
                            }
                            rm.Tracks = rmTracks;
                            releaseMedias.Add(rm);
                        }
                        release.Media = releaseMedias.OrderBy(x => x.MediaNumber).ToArray();
                    }
                }
            }
            sw.Stop();
            return new Library.Models.Pagination.PagedResult<ReleaseList>
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