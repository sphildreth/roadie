using Mapster;
using Microsoft.Extensions.Logging;
using Roadie.Library;
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
                             ILogger<PlayActivityService> logger)
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

        public async Task<OperationResult<UserTrack>> CreatePlayActivity(User roadieUser, TrackStreamInfo streamInfo)
        {
            var sw = Stopwatch.StartNew();

            var track = this.GetTrack(streamInfo.Track.Value);
            if (track == null)
            {
                return new OperationResult<UserTrack>($"CreatePlayActivity: Unable To Find Track [{ streamInfo.Track.Value }]");
            }
            if (!track.IsValid)
            {
                return new OperationResult<UserTrack>($"CreatePlayActivity: Invalid Track. Track Id [{streamInfo.Track.Value}], FilePath [{track.FilePath}], Filename [{track.FileName}]");
            }
            var user = this.GetUser(roadieUser.UserId);
            if (user == null)
            {
                return new OperationResult<UserTrack>($"CreatePlayActivity: Unable To Find User [{ roadieUser.UserId }]");
            }
            var now = DateTime.UtcNow;
            track.PlayedCount = (track.PlayedCount ?? 0) + 1;
            var userTrack = user.TrackRatings.FirstOrDefault(x => x.TrackId == track.Id);
            if (userTrack == null)
            {
                userTrack = new data.UserTrack(now)
                {
                    UserId = user.Id,
                    TrackId = track.Id
                };
                this.DbContext.UserTracks.Add(userTrack);
            }
            userTrack.LastPlayed = now;
            userTrack.PlayedCount++;

            this.CacheManager.ClearRegion(user.CacheRegion);
            this.CacheManager.ClearRegion(track.CacheRegion);
            this.CacheManager.ClearRegion(track.ReleaseMedia.Release.CacheRegion);
            this.CacheManager.ClearRegion(track.ReleaseMedia.Release.Artist.CacheRegion);

            // TODO publish with SignalR 

            //if (!this.RoadieUser.isPrivate ?? false)
            //{
            //    try
            //    {
            //        var hub = GlobalHost.ConnectionManager.GetHubContext<Hubs.PlayActivityHub>();
            //        var releaseArtist = track.releasemedia.release.artist;
            //        artist trackArtist = track.artistId == null ? null : context.artists.FirstOrDefault(x => x.id == track.artistId);
            //        hub.Clients.All.PlayActivity(new PlayActivityListModel
            //        {
            //            releaseTitle = track.releasemedia.release.title,
            //            playedDateDateTime = userTrack.lastPlayed,
            //            userId = this.RoadieUser.roadieId,
            //            userName = this.RoadieUser.username,
            //            releaseId = track.releasemedia.release.roadieId,
            //            trackId = track.roadieId,
            //            IsLocked = (track.isLocked ?? false) || (track.releasemedia.release.isLocked ?? false) || ((trackArtist ?? releaseArtist).isLocked ?? false),
            //            createdDateTime = track.createdDate,
            //            lastUpdatedDateTime = track.lastUpdated,
            //            releasePlayUrl = this.Request.Url.BasePath + "/play/release/" + this.Base64BearerToken + "/" + track.releasemedia.release.roadieId,
            //            rating = track.rating,
            //            userRating = userTrack.rating,
            //            releaseArtistId = releaseArtist.roadieId,
            //            releaseArtistName = releaseArtist.name,
            //            roadieId = track.roadieId,
            //            status = track.status.ToString(),
            //            title = track.title,
            //            trackArtistId = trackArtist == null ? null : trackArtist.roadieId,
            //            trackArtistName = trackArtist == null ? null : trackArtist.name,
            //            trackPlayUrl = this.Request.Url.BasePath + "/play/track/" + this.Base64BearerToken + "/" + track.roadieId,
            //            artistThumbnailUrl = this.Request.Url.BasePath + "/api/v1/image/artist/thumbnail/" + (trackArtist != null ? trackArtist.roadieId : releaseArtist.roadieId),
            //            releaseThumbnailUrl = this.Request.Url.BasePath + "/api/v1/image/release/thumbnail/" + track.releasemedia.release.roadieId,
            //            userThumbnailUrl = this.Request.Url.BasePath + "/api/v1/image/user/thumbnail/" + this.RoadieUser.roadieId
            //        });
            //    }
            //    catch (Exception ex)
            //    {
            //        this.LoggingService.Error(ex.Serialize());
            //    }
            //}

            await this.DbContext.SaveChangesAsync();
            sw.Stop();
            return new OperationResult<UserTrack>
            {
                Data = userTrack.Adapt<UserTrack>(),
                IsSuccess = userTrack != null,
                OperationTime = sw.ElapsedMilliseconds
            };

        }
    }
}