using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
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
    public class PlayActivityService : ServiceBase, IPlayActivityService
    {
        public PlayActivityService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext dbContext,
                             ICacheManager cacheManager,
                             ILogger<StatisticsService> logger)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
        }

        public async Task<Library.Models.Pagination.PagedResult<PlayActivityList>> List(PagedRequest request, User roadieUser = null)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                var result = (from t in this.DbContext.Tracks
                              join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                              join r in this.DbContext.Releases on rm.ReleaseId equals r.Id
                              join trackArtist in this.DbContext.Artists on t.ArtistId equals trackArtist.Id into tas
                              from trackArtist in tas.DefaultIfEmpty()
                              join usertrack in this.DbContext.UserTracks on t.Id equals usertrack.TrackId
                              join u in this.DbContext.Users on usertrack.UserId equals u.Id
                              join releaseArtist in this.DbContext.Artists on r.ArtistId equals releaseArtist.Id
                              where ((roadieUser == null && !(u.IsPrivate ?? false)) || (roadieUser != null && (usertrack != null && usertrack.User.Id == roadieUser.Id)))
                              where (request.FilterValue.Length == 0 || (request.FilterValue.Length > 0 && (
                                        t.Title != null && t.Title.ToLower().Contains(request.Filter.ToLower()) ||
                                        t.AlternateNames != null && t.AlternateNames.ToLower().Contains(request.Filter.ToLower())
                              )))
                              select new PlayActivityList
                              {
                                  Release = new DataToken
                                  {
                                      Text = r.Title,
                                      Value = r.RoadieId.ToString()
                                  },
                                  Track = new DataToken
                                  {
                                      Text = t.Title,
                                      Value = t.RoadieId.ToString()
                                  },
                                  User = new DataToken
                                  {
                                      Text = u.UserName,
                                      Value = u.RoadieId.ToString()
                                  },
                                  Artist = new DataToken
                                  {
                                      Text = releaseArtist.Name,
                                      Value = releaseArtist.RoadieId.ToString()
                                  },
                                  TrackArtist = trackArtist == null ? null : new DataToken
                                  {
                                      Text = trackArtist.Name,
                                      Value = trackArtist.RoadieId.ToString()
                                  },
                                  PlayedDateDateTime = usertrack.LastPlayed,
                                  ReleasePlayUrl = $"{ this.HttpContext.BaseUrl }/play/release/{ r.RoadieId}",
                                  Rating = t.Rating,
                                  UserRating = usertrack.Rating,
                                  TrackPlayUrl = $"{ this.HttpContext.BaseUrl }/play/track/{ t.RoadieId}",
                                  ArtistThumbnail = this.MakeArtistThumbnailImage(trackArtist != null ? trackArtist.RoadieId : releaseArtist.RoadieId),
                                  ReleaseThumbnail = this.MakeReleaseThumbnailImage(r.RoadieId),
                                  UserThumbnail = this.MakeUserThumbnailImage(u.RoadieId)
                              });

                var sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "PlayedDateDateTime", "DESC" } }) : request.OrderValue(null);
                var rowCount = result.Count();
                var rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
                sw.Stop();
                return new Library.Models.Pagination.PagedResult<PlayActivityList>
                {
                    TotalCount = rowCount,
                    CurrentPage = request.PageValue,
                    TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                    OperationTime = sw.ElapsedMilliseconds,
                    Rows = rows
                };
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex);
            }
            return new Library.Models.Pagination.PagedResult<PlayActivityList>();
        }
    }
}