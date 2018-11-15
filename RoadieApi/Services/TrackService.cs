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
    public class TrackService : ServiceBase, ITrackService
    {
        public TrackService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext dbContext,
                             ICacheManager cacheManager,
                             ILogger<StatisticsService> logger)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
        }

        public async Task<Library.Models.Pagination.PagedResult<TrackList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false, Guid? releaseId = null)
        {
            var sw = new Stopwatch();
            sw.Start();

            if (!string.IsNullOrEmpty(request.Sort))
            {
                request.Sort = request.Sort.Replace("createdDate", "createdDateTime");
                request.Sort = request.Sort.Replace("lastUpdated", "lastUpdatedDateTime");
                request.Sort = request.Sort.Replace("artist", "TrackArtistName, ReleaseArtistName");
            }

            var resultQuery = (from t in this.DbContext.Tracks
                               join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                               join r in this.DbContext.Releases on rm.ReleaseId equals r.Id
                               join trackArtist in this.DbContext.Artists on t.ArtistId equals trackArtist.Id into tas
                               from trackArtist in tas.DefaultIfEmpty()
                               join releaseArtist in this.DbContext.Artists on r.ArtistId equals releaseArtist.Id
                               where (t.Hash != null || releaseId != null)
                               where (request.FilterMinimumRating == null || t.Rating >= request.FilterMinimumRating.Value)
                               where (request.FilterToArtistId == null || r.Artist.RoadieId == request.FilterToArtistId)
                               where (request.FilterValue == "" || (t.Title.Contains(request.FilterValue) || t.AlternateNames.Contains(request.FilterValue)))
                               where (releaseId == null || (releaseId != null && r.RoadieId == releaseId))
                               select new { t, rm, r, trackArtist, releaseArtist });

            if (!string.IsNullOrEmpty(request.FilterValue))
            {
                if (request.FilterValue.StartsWith("#"))
                {
                    // Find any releases by tags
                    var tagValue = request.FilterValue.Replace("#", "");
                    resultQuery = resultQuery.Where(x => x.t.Tags != null && x.t.Tags.Contains(tagValue));
                }
            }
            var result = resultQuery.Select(x =>
                          new TrackList
                          {
                              DatabaseId = x.t.Id,
                              Id = x.t.RoadieId,
                              Track = new DataToken
                              {
                                  Text = x.t.Title,
                                  Value = x.t.RoadieId.ToString()
                              },
                              Release = new DataToken
                              {
                                  Text = x.r.Title,
                                  Value = x.r.RoadieId.ToString()
                              },
                              Artist = new DataToken
                              {
                                  Text = x.releaseArtist.Name,
                                  Value = x.releaseArtist.RoadieId.ToString()
                              },
                              TrackArtist = x.trackArtist != null ? new DataToken
                              {
                                  Text = x.trackArtist.Name,
                                  Value = x.trackArtist.RoadieId.ToString()
                              } : null,
                              TrackNumber = x.t.TrackNumber,
                              MediaNumber = x.rm.MediaNumber,
                              CreatedDate = x.t.CreatedDate,
                              LastUpdated = x.t.LastUpdated,
                              ReleaseThumbnail = this.MakeReleaseThumbnailImage(x.r.RoadieId),
                              Duration = x.t.Duration,
                              Rating = x.t.Rating,
                              ArtistThumbnail = this.MakeArtistThumbnailImage(x.releaseArtist.RoadieId),
                              Title = x.t.Title,
                              TrackArtistThumbnail = x.trackArtist != null ? this.MakeArtistThumbnailImage(x.trackArtist.RoadieId) : null,
                              TrackPlayUrl = $"{ this.HttpContext.BaseUrl }/play/track/{ x.t.RoadieId }",
                              Thumbnail = this.MakeTrackThumbnailImage(x.t.RoadieId)
                          });
            string sortBy = null;

            var rowCount = result.Count();
            TrackList[] rows = null;

            if (doRandomize ?? false)
            {
                request.Limit = request.LimitValue > roadieUser.RandomReleaseLimit ? roadieUser.RandomReleaseLimit : request.LimitValue;
                rows = result.OrderBy(x => Guid.NewGuid()).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
            }
            else
            {
                if (request.Action == User.ActionKeyUserRated)
                {
                    sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "UserTrack.Rating", "DESC" }, { "MediaNumber", "ASC" }, { "TrackNumber", "ASC" } }) : request.OrderValue(null);
                }
                else
                {
                    sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "Release.Text", "ASC" }, { "MediaNumber", "ASC" }, { "TrackNumber", "ASC" } }) : request.OrderValue(null);
                }
                rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
            }

            if (rows.Any() && roadieUser != null)
            {
                foreach (var userTrack in this.GetUser(roadieUser.UserId).TrackRatings)
                {
                    var row = rows.FirstOrDefault(x => x.DatabaseId == userTrack.TrackId);
                    if (row != null)
                    {
                        row.UserRating = new UserTrack
                        {
                            IsDisliked = userTrack.IsDisliked ?? false,
                            IsFavorite = userTrack.IsFavorite ?? false,
                            Rating = userTrack.Rating,
                            LastPlayed = userTrack.LastPlayed,
                            PlayedCount = userTrack.PlayedCount
                        };
                    }
                }
            }

            if (rows.Any())
            {
                foreach (var row in rows)
                {
                    row.PlayedCount = (from ut in this.DbContext.UserTracks
                                       join tr in this.DbContext.Tracks on ut.TrackId equals tr.Id
                                       where ut.TrackId == row.DatabaseId
                                       select ut.PlayedCount).Sum() ?? 0;

                    row.FavoriteCount = (from ut in this.DbContext.UserTracks
                                         join tr in this.DbContext.Tracks on ut.TrackId equals tr.Id
                                         where ut.TrackId == row.DatabaseId
                                         where ut.IsFavorite ?? false
                                         select ut.Id).Count();
                }
            }

            sw.Stop();
            return new Library.Models.Pagination.PagedResult<TrackList>
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