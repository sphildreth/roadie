using Mapster;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadie.Api.Hubs;
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
        protected IHubContext<PlayActivityHub> PlayActivityHub { get; }

        public PlayActivityService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext dbContext,
                             ICacheManager cacheManager,
                             ILogger<PlayActivityService> logger,
                             IHubContext<PlayActivityHub> playHubContext)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
            this.PlayActivityHub = playHubContext;
        }

        public Task<Library.Models.Pagination.PagedResult<PlayActivityList>> List(PagedRequest request, User roadieUser = null, DateTime? newerThan = null)
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
                              where(newerThan == null || usertrack.LastPlayed >= newerThan)
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
                                  TrackPlayUrl = $"{ this.HttpContext.BaseUrl }/play/track/{ t.RoadieId}.mp3",
                                  ArtistThumbnail = this.MakeArtistThumbnailImage(trackArtist != null ? trackArtist.RoadieId : releaseArtist.RoadieId),
                                  ReleaseThumbnail = this.MakeReleaseThumbnailImage(r.RoadieId),
                                  UserThumbnail = this.MakeUserThumbnailImage(u.RoadieId)
                              });

                var sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "PlayedDateDateTime", "DESC" } }) : request.OrderValue(null);
                var rowCount = result.Count();
                var rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
                sw.Stop();
                return Task.FromResult(new Library.Models.Pagination.PagedResult<PlayActivityList>
                {
                    TotalCount = rowCount,
                    CurrentPage = request.PageValue,
                    TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                    OperationTime = sw.ElapsedMilliseconds,
                    Rows = rows
                });
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex);
            }
            return Task.FromResult(new Library.Models.Pagination.PagedResult<PlayActivityList>());
        }

        public async Task<OperationResult<PlayActivityList>> CreatePlayActivity(User roadieUser, TrackStreamInfo streamInfo)
        {
            var sw = Stopwatch.StartNew();

            var track = this.GetTrack(streamInfo.Track.Value);
            if (track == null)
            {
                return new OperationResult<PlayActivityList>($"CreatePlayActivity: Unable To Find Track [{ streamInfo.Track.Value }]");
            }
            if (!track.IsValid)
            {
                return new OperationResult<PlayActivityList>($"CreatePlayActivity: Invalid Track. Track Id [{streamInfo.Track.Value}], FilePath [{track.FilePath}], Filename [{track.FileName}]");
            }
            data.UserTrack userTrack = null;
            var now = DateTime.UtcNow;
            track.PlayedCount = (track.PlayedCount ?? 0) + 1;
            var user = this.GetUser(roadieUser?.UserId);
            if (user != null)
            {
                userTrack = user.TrackRatings.FirstOrDefault(x => x.TrackId == track.Id);
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
            }

            var release = this.GetRelease(track.ReleaseMedia.Release.RoadieId);
            release.LastPlayed = now;
            release.PlayedCount++;

            var artist = this.GetArtist(release.Artist.RoadieId);
            artist.LastPlayed = now;
            artist.PlayedCount++;

            this.CacheManager.ClearRegion(track.CacheRegion);
            this.CacheManager.ClearRegion(track.ReleaseMedia.Release.CacheRegion);
            this.CacheManager.ClearRegion(track.ReleaseMedia.Release.Artist.CacheRegion);

            var pl = new PlayActivityList
            {
                Artist = new DataToken
                {
                    Text = track.ReleaseMedia.Release.Artist.Name,
                    Value = track.ReleaseMedia.Release.Artist.RoadieId.ToString()
                },
                TrackArtist = track.TrackArtist == null ? null : new DataToken
                {
                    Text = track.TrackArtist.Name,
                    Value = track.TrackArtist.RoadieId.ToString()
                },
                Release = new DataToken
                {
                    Text = track.ReleaseMedia.Release.Title,
                    Value = track.ReleaseMedia.Release.RoadieId.ToString()
                },
                Track = new DataToken
                {
                    Text = track.Title,
                    Value = track.RoadieId.ToString()
                },
                User = new DataToken
                {
                    Text = roadieUser.UserName,
                    Value = roadieUser.UserId.ToString()
                },
                PlayedDateDateTime = userTrack?.LastPlayed,
                ReleasePlayUrl = $"{ this.HttpContext.BaseUrl }/play/release/{ track.ReleaseMedia.Release.RoadieId}",
                Rating = track.Rating,
                UserRating = userTrack?.Rating,
                TrackPlayUrl = $"{ this.HttpContext.BaseUrl }/play/track/{ track.RoadieId}.mp3",
                ArtistThumbnail = this.MakeArtistThumbnailImage(track.TrackArtist != null ? track.TrackArtist.RoadieId : track.ReleaseMedia.Release.Artist.RoadieId),
                ReleaseThumbnail = this.MakeReleaseThumbnailImage(track.ReleaseMedia.Release.RoadieId),
                UserThumbnail = this.MakeUserThumbnailImage(roadieUser.UserId)
            };

            if (!roadieUser.IsPrivate)
            {
                try
                {
                    await this.PlayActivityHub.Clients.All.SendAsync("SendActivity",pl);
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex);
                }
            }

            await this.DbContext.SaveChangesAsync();
            sw.Stop();
            return new OperationResult<PlayActivityList>
            {
                Data = pl,
                IsSuccess = userTrack != null,
                OperationTime = sw.ElapsedMilliseconds
            };

        }
    }
}