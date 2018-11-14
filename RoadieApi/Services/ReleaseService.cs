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
using Roadie.Library.Models.Releases;
using Roadie.Library.Models.Statistics;
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

        public async Task<Library.Models.Pagination.PagedResult<ReleaseList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false, IEnumerable<string> includes = null)
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

            var result = (from r in this.DbContext.Releases.Include("Artist")
                               join a in this.DbContext.Artists on r.ArtistId equals a.Id
                               where (request.FilterMinimumRating == null || r.Rating >= request.FilterMinimumRating.Value)
                               where (request.FilterToArtistId == null || r.Artist.RoadieId == request.FilterToArtistId)
                               where (request.FilterValue == "" || (r.Title.Contains(request.FilterValue) || r.AlternateNames.Contains(request.FilterValue)))
                               select new ReleaseList
                                {
                                    DatabaseId = r.Id,
                                    Id = r.RoadieId,
                                    Artist = new DataToken
                                    {
                                        Value = r.Artist.RoadieId.ToString(),
                                        Text = r.Artist.Name
                                    },
                                    ArtistThumbnail = this.MakeArtistThumbnailImage(r.Artist.RoadieId),
                                    Rating = r.Rating,
                                    ReleasePlayUrl = $"{ this.HttpContext.BaseUrl }/play/release/{ r.RoadieId}",
                                    LibraryStatus = r.LibraryStatus,
                                    ReleaseDateDateTime = r.ReleaseDate,
                                    Release = new DataToken
                                    {
                                        Text = r.Title,
                                        Value = r.RoadieId.ToString()
                                    },
                                    Status = r.Status,
                                    TrackCount = r.TrackCount,
                                    CreatedDate = r.CreatedDate,
                                    LastUpdated = r.LastUpdated,
                                    TrackPlayedCount = (from ut in this.DbContext.UserTracks
                                                        join t in this.DbContext.Tracks on ut.TrackId equals t.Id
                                                        join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                        join rl in this.DbContext.Releases on rm.ReleaseId equals rl.Id
                                                        where rl.Id == r.Id
                                                        select ut.PlayedCount ?? 0).Sum(),
                                    Thumbnail = this.MakeReleaseThumbnailImage(r.RoadieId)
                                }).Distinct();

            ReleaseList[] rows = null;

            var rowCount = result.Count();

            if (doRandomize ?? false)
            {
                request.Limit = request.LimitValue > roadieUser.RandomReleaseLimit ? roadieUser.RandomReleaseLimit : request.LimitValue;
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
            if (rows.Any() && roadieUser != null)
            {
                foreach (var userReleaseRatings in this.GetUser(roadieUser.UserId).ReleaseRatings.Where(x => rows.Select(r => r.DatabaseId).Contains(x.ReleaseId)))
                {
                    var row = rows.FirstOrDefault(x => x.DatabaseId == userReleaseRatings.ReleaseId);
                    if (row != null)
                    {
                        row.UserRating = new UserRelease
                        {
                            IsDisliked = userReleaseRatings.IsDisliked ?? false,
                            IsFavorite = userReleaseRatings.IsFavorite ?? false,
                            Rating = userReleaseRatings.Rating
                        };
                    }
                }
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
                                            where ut.UserId == roadieUser.Id
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
                                t.UserTrack = new UserTrack
                                {
                                    Rating = userRating.Rating,
                                    IsFavorite = userRating.IsFavorite ?? false,
                                    IsDisliked = userRating.IsDisliked ?? false
                                };
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

        public async Task<OperationResult<Release>> ById(User roadieUser, Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = string.Format("urn:release_by_id_operation:{0}:{1}", id, includes == null ? "0" : string.Join("|", includes));
            var result = await this.CacheManager.GetAsync<OperationResult<Release>>(cacheKey, async () => {
                return await this.ReleaseByIdAction(id, includes);
            }, data.Artist.CacheRegionUrn(id));
            if (result?.Data != null)
            {
                var release = this.GetRelease(id);
                result.Data.UserBookmark = this.GetUserBookmarks(roadieUser).FirstOrDefault(x => x.Type == BookmarkType.Release && x.Bookmark.Value == release.RoadieId.ToString());

                if (result.Data.Medias != null)
                {
                    var releaseTrackIds = result.Data.Medias.SelectMany(x => x.Tracks).Select(x => x.Id);
                    var releaseUserTracks = (from ut in this.DbContext.UserTracks
                                             join t in this.DbContext.Tracks on ut.TrackId equals t.Id
                                             where ut.UserId == roadieUser.Id
                                             where (from x in releaseTrackIds select x).Contains(t.RoadieId)
                                             select new
                                             {
                                                 t,
                                                 ut
                                             }).ToArray();
                    if (releaseUserTracks != null && releaseUserTracks.Any())
                    {
                        foreach (var releaseUserTrack in releaseUserTracks)
                        {
                            foreach (var media in result.Data.Medias)
                            {
                                var releaseTrack = media.Tracks.FirstOrDefault(x => x.Id == releaseUserTrack.t.RoadieId);
                                if (releaseTrack != null)
                                {
                                    releaseTrack.UserTrack = new UserTrack
                                    {
                                        Rating = releaseUserTrack.ut.Rating,
                                        IsDisliked = releaseUserTrack.ut.IsDisliked ?? false,
                                        IsFavorite = releaseUserTrack.ut.IsFavorite ?? false,
                                        LastPlayed = releaseUserTrack.ut.LastPlayed,
                                        PlayedCount = releaseUserTrack.ut.PlayedCount
                                    };
                                }
                            }
                        }
                    }
                }
                var userRelease = this.DbContext.UserReleases.FirstOrDefault(x => x.ReleaseId == release.Id && x.UserId == roadieUser.Id);
                if (userRelease != null)
                {
                    result.Data.UserRating = new UserRelease
                    {
                        IsDisliked = userRelease.IsDisliked ?? false,
                        IsFavorite = userRelease.IsFavorite ?? false,
                        Rating = userRelease.Rating
                    };
                }
            }
            sw.Stop();
            return new OperationResult<Release>(result.Messages)
            {
                Data = result?.Data,
                Errors = result?.Errors,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        private async Task<OperationResult<Release>> ReleaseByIdAction(Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();

            var release = this.GetRelease(id);

            if (release == null)
            {
                return new OperationResult<Release>(true, string.Format("Release Not Found [{0}]", id));
            }
            var result = release.Adapt<Release>();
            result.Artist = new DataToken
            {
                Text = release.Artist.Name,
                Value = release.Artist.RoadieId.ToString()
            };
            result.ArtistThumbnail = this.MakeArtistThumbnailImage(release.Artist.RoadieId);
            result.Thumbnail = this.MakeReleaseThumbnailImage(release.RoadieId);
            result.ReleasePlayUrl = $"{ this.HttpContext.BaseUrl }/play/release/{ release.RoadieId}";
            result.Profile = release.Profile;
            result.ReleaseDate = release.ReleaseDate.Value;
            result.MediaCount = release.MediaCount;
            result.TrackCount = release.TrackCount;
            result.CreatedDate = release.CreatedDate;
            result.LastUpdated = release.LastUpdated;
            result.AlternateNames = release.AlternateNames;
            result.Tags = release.Tags;
            result.URLs = release.URLs;

            if (release.SubmissionId.HasValue)
            {
                var submission = this.DbContext.Submissions.Include(x => x.User).FirstOrDefault(x => x.Id == release.SubmissionId);
                if (submission != null)
                {
                    if (!submission.User.IsPrivate ?? false)
                    {
                        result.Submission = new ReleaseSubmission
                        {
                            User = new DataToken
                            {
                                Text = submission.User.UserName,
                                Value = submission.User.RoadieId.ToString()
                            },
                            UserThumbnail = this.MakeUserThumbnailImage(submission.User.RoadieId),
                            SubmittedDate = submission.CreatedDate
                        };

                    }
                }
            }
            result.Genres = release.Genres.Select(x => new DataToken
            {
                Text = x.Genre.Name,
                Value = x.Genre.RoadieId.ToString()
            });
            if (includes != null && includes.Any())
            {
                if (includes.Contains("stats"))
                {
                    var releaseTracks = (from r in this.DbContext.Releases
                                         join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                         join t in this.DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                         where r.Id == release.Id
                                         select new
                                         {
                                             id = t.Id,
                                             size = t.FileSize,
                                             time = t.Duration,
                                             isMissing = t.Hash == null
                                         });
                    var releaseMedias = (from r in this.DbContext.Releases
                                         join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                         where r.Id == release.Id
                                         select new
                                         {
                                             rm.Id,
                                             rm.MediaNumber
                                         });
                    var releaseTime = releaseTracks.Sum(x => x.time);
                    var releaseStats = new ReleaseStatistics
                    {
                        MediaCount = releaseMedias.Count(),
                        MissingTrackCount = releaseTracks.Where(x => x.isMissing).Count(),
                        TrackCount = releaseTracks.Count(),
                        TrackPlayedCount = (from t in releaseTracks
                                                   join ut in this.DbContext.UserTracks on t.id equals ut.TrackId
                                                   select ut.PlayedCount).Sum(),
                        TrackSize = releaseTracks.Sum(x => (long?)x.size).ToFileSize(),
                        TrackTime = releaseTracks.Any() ? TimeSpan.FromSeconds(Math.Floor((double)releaseTime / 1000)).ToString(@"hh\:mm\:ss") : "--:--"
                    };
                    result.MaxMediaNumber = releaseMedias.Max(x => x.MediaNumber);
                    result.Statistics = releaseStats;
                    result.MediaCount = release.MediaCount ?? (short?)releaseStats.MediaCount;
                }
                if (includes.Contains("images"))
                {
                    var releaseImages = this.DbContext.Images.Where(x => x.ReleaseId == release.Id).Select(x => MakeImage(x.RoadieId, this.Configuration.LargeThumbnails.Width, this.Configuration.LargeThumbnails.Height)).ToArray();
                    if(releaseImages != null && releaseImages.Any())
                    {
                        result.Images = releaseImages;
                    }
                }
                if (includes.Contains("labels"))
                {
                    var releaseLabels = (from l in this.DbContext.Labels
                                         join rl in this.DbContext.ReleaseLabels on l.Id equals rl.LabelId
                                         where rl.ReleaseId == release.Id
                                         orderby rl.BeginDate, l.Name
                                         select new
                                         {
                                             l,
                                             rl
                                         }).ToArray();
                    if (releaseLabels != null)
                    {
                        var labels = new List<ReleaseLabel>();
                        foreach (var releaseLabel in releaseLabels)
                        {
                            var rl = new ReleaseLabel
                            {
                                BeginDate = releaseLabel.rl.BeginDate,
                                EndDate = releaseLabel.rl.EndDate,
                                CatalogNumber = releaseLabel.rl.CatalogNumber,
                                Label = new DataToken
                                {
                                    Text = releaseLabel.l.Name,
                                    Value = releaseLabel.l.RoadieId.ToString()
                                }
                            };
                            labels.Add(rl);
                        }
                        result.Labels = labels;
                    }
                }
                if (includes.Contains("collections"))
                {
                    var releaseCollections = this.DbContext.CollectionReleases.Include(x => x.Collection).Where(x => x.ReleaseId == release.Id).OrderBy(x => x.ListNumber).ToArray();
                    if (releaseCollections != null)
                    {
                        var collections = new List<ReleaseInCollection>();
                        foreach (var releaseCollection in releaseCollections)
                        {                          
                            collections.Add(new ReleaseInCollection
                            {
                                Collection = new DataToken
                                {
                                    Text = releaseCollection.Collection.Name,
                                    Value = releaseCollection.Collection.RoadieId.ToString()
                                },
                                CollectionImage = this.MakeCollectionThumbnailImage(releaseCollection.Collection.RoadieId),
                                CollectionType = releaseCollection.Collection.CollectionType,
                                ListNumber = releaseCollection.ListNumber
                            });
                        }
                        result.Collections = collections;
                    }
                }
                if (includes.Contains("tracks"))
                {
                    var releaseTracks = (from r in this.DbContext.Releases
                                         join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                         join t in this.DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                         join a in this.DbContext.Artists on t.ArtistId equals a.Id into tas
                                         from a in tas.DefaultIfEmpty()
                                         where r.Id == release.Id
                                         orderby rm.MediaNumber, t.TrackNumber
                                         select new
                                         {
                                             t,
                                             releaseMedia = rm,
                                             trackArtist = a
                                         }).ToArray();
                    var releaseMedias = new List<ReleaseMediaList>();
                    var releaseTrackIds = releaseTracks.Select(x => x.t.Id).ToList();
                    foreach (var releaseMedia in releaseTracks.Select(x => x.releaseMedia).Distinct())
                    {
                        var rm = releaseMedia.Adapt<ReleaseMediaList>();
                        var rmTracks = new List<TrackList>();
                        foreach (var track in releaseTracks.Where(x => x.t.ReleaseMediaId == releaseMedia.Id))
                        {
                            var t = track.t.Adapt<TrackList>();
                            t.TrackArtist = track.trackArtist != null ? new DataToken
                            {
                                Text = track.trackArtist.Name,
                                Value = track.trackArtist.RoadieId.ToString()
                            } : null;
                            t.TrackArtistThumbnail = track.trackArtist != null ? this.MakeArtistThumbnailImage(track.trackArtist.RoadieId) : null;
                            t.TrackPlayUrl = $"{ this.HttpContext.BaseUrl }/play/track/{ t.Id}";                           
                            rmTracks.Add(t);
                        }
                        rm.Tracks = rmTracks;
                        releaseMedias.Add(rm);
                    }
                    result.Medias = releaseMedias;
                 }
            }
            sw.Stop();
            return new OperationResult<Release>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            };
        }
    }
}