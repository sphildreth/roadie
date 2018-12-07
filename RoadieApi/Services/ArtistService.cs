using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
#pragma warning disable 1998

    public class ArtistService : ServiceBase, IArtistService
    {
        private ICollectionService CollectionService { get; } = null;
        private IPlaylistService PlaylistService { get; } = null;
        private IBookmarkService BookmarkService { get; } = null;

        public ArtistService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext context,
                             ICacheManager cacheManager,
                             ILogger<ArtistService> logger,
                             ICollectionService collectionService,
                             IPlaylistService playlistService,
                             IBookmarkService bookmarkService
            )
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
            this.CollectionService = collectionService;
            this.PlaylistService = playlistService;
            this.BookmarkService = bookmarkService;
        }

        public async Task<OperationResult<Artist>> ById(User roadieUser, Guid id, IEnumerable<string> includes)
        {
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = string.Format("urn:artist_by_id_operation:{0}:{1}", id, includes == null ? "0" : string.Join("|", includes));
            var result = await this.CacheManager.GetAsync<OperationResult<Artist>>(cacheKey, async () =>
            {
                tsw.Restart();
                var rr = await this.ArtistByIdAction(id, includes);
                tsw.Stop();
                timings.Add("ArtistByIdAction", tsw.ElapsedMilliseconds);
                return rr;

            }, data.Artist.CacheRegionUrn(id));
            if (result?.Data != null && roadieUser != null)
            {
                tsw.Restart();
                var artist = this.GetArtist(id);
                tsw.Stop();
                timings.Add("GetArtist", tsw.ElapsedMilliseconds);
                tsw.Restart();
                var userBookmarkResult = await this.BookmarkService.List(roadieUser, new PagedRequest(), false, BookmarkType.Artist);
                if (userBookmarkResult.IsSuccess)
                {
                    result.Data.UserBookmarked = userBookmarkResult?.Rows?.FirstOrDefault(x => x.Bookmark.Value == artist.RoadieId.ToString()) != null;
                }
                tsw.Stop();
                timings.Add("userBookmarkResult", tsw.ElapsedMilliseconds);
                tsw.Restart();
                var userArtist = this.DbContext.UserArtists.FirstOrDefault(x => x.ArtistId == artist.Id && x.UserId == roadieUser.Id);
                if (userArtist != null)
                {
                    result.Data.UserRating = new UserArtist
                    {
                        IsDisliked = userArtist.IsDisliked ?? false,
                        IsFavorite = userArtist.IsFavorite ?? false,
                        Rating = userArtist.Rating
                    };
                }
                tsw.Stop();
                timings.Add("userArtist", tsw.ElapsedMilliseconds);
            }
            sw.Stop();
            timings.Add("operation", sw.ElapsedMilliseconds);
            this.Logger.LogDebug("ById Timings: id [{0}], includes [{1}], timings [{3}]", id, includes, JsonConvert.SerializeObject(timings));
            return new OperationResult<Artist>(result.Messages)
            {
                Data = result?.Data,
                Errors = result?.Errors,
                IsNotFoundResult = result?.IsNotFoundResult ?? false,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        private async Task<OperationResult<Artist>> ArtistByIdAction(Guid id, IEnumerable<string> includes)
        {
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            var sw = Stopwatch.StartNew();
            sw.Start();

            tsw.Restart();
            var artist = this.GetArtist(id);
            tsw.Stop();
            timings.Add("getArtist", tsw.ElapsedMilliseconds);

            if (artist == null)
            {
                return new OperationResult<Artist>(true, string.Format("Artist Not Found [{0}]", id));
            }
            tsw.Restart();
            var result = artist.Adapt<Artist>();
            tsw.Stop();
            timings.Add("adaptArtist", tsw.ElapsedMilliseconds);
            result.Thumbnail = base.MakeArtistThumbnailImage(id);
            result.MediumThumbnail = base.MakeThumbnailImage(id, "artist", this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height);
            tsw.Restart();
            result.Genres = artist.Genres.Select(x => new DataToken { Text = x.Genre.Name, Value = x.Genre.RoadieId.ToString() });
            tsw.Stop();
            timings.Add("genres", tsw.ElapsedMilliseconds);

            if (includes != null && includes.Any())
            {
                if (includes.Contains("releases"))
                {
                    var dtoReleases = new List<ReleaseList>();
                    foreach (var release in this.DbContext.Releases.Include("Medias").Include("Medias.Tracks").Include("Medias.Tracks").Where(x => x.ArtistId == artist.Id).ToArray())
                    {
                        var releaseList = release.Adapt<ReleaseList>();
                        releaseList.Thumbnail = base.MakeReleaseThumbnailImage(release.RoadieId);
                        var dtoReleaseMedia = new List<ReleaseMediaList>();
                        if (includes.Contains("tracks"))
                        {
                            foreach (var releasemedia in release.Medias.OrderBy(x => x.MediaNumber).ToArray())
                            {
                                var dtoMedia = releasemedia.Adapt<ReleaseMediaList>();
                                var tracks = new List<TrackList>();
                                foreach (var t in this.DbContext.Tracks.Where(x => x.ReleaseMediaId == releasemedia.Id).OrderBy(x => x.TrackNumber).ToArray())
                                {
                                    var track = t.Adapt<TrackList>();
                                    ArtistList trackArtist = null;
                                    if (t.ArtistId.HasValue)
                                    {
                                        var ta = this.DbContext.Artists.FirstOrDefault(x => x.Id == t.ArtistId.Value);
                                        if (ta != null)
                                        {
                                            trackArtist = ArtistList.FromDataArtist(ta, this.MakeArtistThumbnailImage(ta.RoadieId));
                                        }
                                    }
                                    track.TrackArtist = trackArtist;
                                    tracks.Add(track);
                                }
                                dtoMedia.Tracks = tracks;
                                dtoReleaseMedia.Add(dtoMedia);
                            }
                        }
                        releaseList.Media = dtoReleaseMedia;
                        dtoReleases.Add(releaseList);
                    }
                    result.Releases = dtoReleases;
                }
                if (includes.Contains("stats"))
                {
                    tsw.Restart();

                    // TODO this should be on artist properties to speed up fetch times

                    var artistTracks = (from r in this.DbContext.Releases
                                        join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                        join t in this.DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                        where r.ArtistId == artist.Id
                                        select new
                                        {
                                            t.Id,
                                            size = t.FileSize,
                                            time = t.Duration,
                                            isMissing = t.Hash == null
                                        });
                    var validCartistTracks = artistTracks.Where(x => !x.isMissing);
                    var trackTime = validCartistTracks.Sum(x => x.time);
                    result.Statistics = new CollectionStatistics
                    {
                        FileSize = artistTracks.Sum(x => (long?)x.size).ToFileSize(),
                        MissingTrackCount = artistTracks.Where(x => x.isMissing).Count(),
                        ReleaseCount = artist.ReleaseCount,
                        ReleaseMediaCount = (from r in this.DbContext.Releases
                                             join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                             where r.ArtistId == artist.Id
                                             select rm.Id).Count(),
                        TrackTime = validCartistTracks.Any() ? TimeSpan.FromSeconds(Math.Floor((double)trackTime / 1000)).ToString(@"dd\:hh\:mm\:ss") : "--:--",
                        TrackCount = validCartistTracks.Count(),
                        TrackPlayedCount = artist.PlayedCount
                    };
                    tsw.Stop();
                    timings.Add("stats", tsw.ElapsedMilliseconds);
                }
                if (includes.Contains("images"))
                {
                    tsw.Restart();
                    result.Images = this.DbContext.Images.Where(x => x.ArtistId == artist.Id).Select(x => MakeFullsizeImage(x.RoadieId, x.Caption)).ToArray();
                    tsw.Stop();
                    timings.Add("images", tsw.ElapsedMilliseconds);
                }
                if (includes.Contains("associatedartists"))
                {
                    tsw.Restart();
                    var associatedWithArtists = (from aa in this.DbContext.ArtistAssociations
                                                    join a in this.DbContext.Artists on aa.AssociatedArtistId equals a.Id
                                                    where aa.ArtistId == artist.Id
                                                     select new ArtistList
                                                     {
                                                         DatabaseId = a.Id,
                                                         Id = a.RoadieId,
                                                         Artist = new DataToken
                                                         {
                                                             Text = a.Name,
                                                             Value = a.RoadieId.ToString()
                                                         },
                                                         Thumbnail = this.MakeArtistThumbnailImage(a.RoadieId),
                                                         Rating = a.Rating,
                                                         CreatedDate = a.CreatedDate,
                                                         LastUpdated = a.LastUpdated,
                                                         LastPlayed = a.LastPlayed,
                                                         PlayedCount = a.PlayedCount,
                                                         ReleaseCount = a.ReleaseCount,
                                                         TrackCount = a.TrackCount,
                                                         SortName = a.SortName
                                                     }).ToArray();

                    var associatedArtists = (from aa in this.DbContext.ArtistAssociations
                                             join a in this.DbContext.Artists on aa.ArtistId equals a.Id
                                             where aa.AssociatedArtistId == artist.Id
                                             select new ArtistList
                                             {
                                                 DatabaseId = a.Id,
                                                 Id = a.RoadieId,
                                                 Artist = new DataToken
                                                 {
                                                     Text = a.Name,
                                                     Value = a.RoadieId.ToString()
                                                 },
                                                 Thumbnail = this.MakeArtistThumbnailImage(a.RoadieId),
                                                 Rating = a.Rating,
                                                 CreatedDate = a.CreatedDate,
                                                 LastUpdated = a.LastUpdated,
                                                 LastPlayed = a.LastPlayed,
                                                 PlayedCount = a.PlayedCount,
                                                 ReleaseCount = a.ReleaseCount,
                                                 TrackCount = a.TrackCount,
                                                 SortName = a.SortName
                                             }).ToArray();

                    result.AssociatedArtists = associatedArtists.Union(associatedWithArtists).OrderBy(x => x.SortName);
                    tsw.Stop();
                    timings.Add("associatedartists", tsw.ElapsedMilliseconds);

                }
                if (includes.Contains("collections"))
                {
                    tsw.Restart();
                    var collectionPagedRequest = new PagedRequest
                    {
                        Limit = 100                        
                    };
                    var r = await this.CollectionService.List(roadieUser: null,
                                                              request: collectionPagedRequest, artistId: artist.RoadieId);
                    if (r.IsSuccess)
                    {
                        result.CollectionsWithArtistReleases = r.Rows.ToArray();
                    }
                    tsw.Stop();
                    timings.Add("collections", tsw.ElapsedMilliseconds);
                }
                if (includes.Contains("playlists"))
                {
                    tsw.Restart();
                    var pg = new PagedRequest
                    {
                        FilterToArtistId = artist.RoadieId
                    };
                    var r = await this.PlaylistService.List(pg);
                    if (r.IsSuccess)
                    {
                        result.PlaylistsWithArtistReleases = r.Rows.ToArray();
                    }
                    tsw.Stop();
                    timings.Add("playlists", tsw.ElapsedMilliseconds);
                }
                if (includes.Contains("contributions"))
                {
                    tsw.Restart();
                    result.ArtistContributionReleases = (from t in this.DbContext.Tracks
                                                         join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                         join r in this.DbContext.Releases.Include(x => x.Artist) on rm.ReleaseId equals r.Id
                                                         where t.ArtistId == artist.Id
                                                         group r by r.Id into rr
                                                         select rr)
                                                         .ToArray()
                                                         .Select(rr => rr.First())
                                                         .Select(r => ReleaseList.FromDataRelease(r, r.Artist, this.HttpContext.BaseUrl, MakeArtistThumbnailImage(r.Artist.RoadieId), MakeReleaseThumbnailImage(r.RoadieId)))
                                                         .ToArray().OrderBy(x => x.Release.Text).ToArray();
                    result.ArtistContributionReleases = result.ArtistContributionReleases.Any() ? result.ArtistContributionReleases : null;
                    tsw.Stop();
                    timings.Add("contributions", tsw.ElapsedMilliseconds);
                }
                if (includes.Contains("labels"))
                {
                    tsw.Restart();                   
                    result.ArtistLabels = (from l in this.DbContext.Labels
                                           join rl in this.DbContext.ReleaseLabels on l.Id equals rl.LabelId
                                           join r in this.DbContext.Releases on rl.ReleaseId equals r.Id
                                           where r.ArtistId == artist.Id
                                           orderby l.SortName
                                           select new LabelList
                                           {
                                               Id = rl.RoadieId,
                                               Label = new DataToken
                                               {
                                                   Text = l.Name,
                                                   Value = l.RoadieId.ToString()
                                               },
                                               SortName = l.SortName,
                                               CreatedDate = l.CreatedDate,
                                               LastUpdated = l.LastUpdated,
                                               ArtistCount = l.ArtistCount,
                                               ReleaseCount = l.ReleaseCount,
                                               TrackCount = l.TrackCount,
                                               Thumbnail = MakeLabelThumbnailImage(l.RoadieId)
                                           }).ToArray().GroupBy(x => x.Label.Value).Select(x => x.First()).OrderBy(x => x.SortName).ThenBy(x => x.Label.Text).ToArray();
                    result.ArtistLabels = result.ArtistLabels.Any() ? result.ArtistLabels : null;
                    tsw.Stop();
                    timings.Add("labels", tsw.ElapsedMilliseconds);
                }
            }
            sw.Stop();
            timings.Add("operation", sw.ElapsedMilliseconds);
            this.Logger.LogDebug("ArtistByIdAction Timings: id [{0}], includes [{1}], timings [{3}]", id, includes, JsonConvert.SerializeObject(timings));

            return new OperationResult<Artist>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<Library.Models.Pagination.PagedResult<ArtistList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false, bool? onlyIncludeWithReleases = true)
        {
            var sw = new Stopwatch();
            sw.Start();

            int[] favoriteArtistIds = new int[0];
            if(request.FilterFavoriteOnly)
            {
                favoriteArtistIds = (from a in this.DbContext.Artists
                                     join ua in this.DbContext.UserArtists on a.Id equals ua.ArtistId
                                     where ua.IsFavorite ?? false
                                     where (roadieUser == null || ua.UserId == roadieUser.Id)
                                     select a.Id
                                     ).ToArray();
            }
            var onlyWithReleases = onlyIncludeWithReleases ?? true;
            var result = (from a in this.DbContext.Artists
                          where (!onlyWithReleases || a.ReleaseCount > 0)
                          where (request.FilterToArtistId == null || a.RoadieId == request.FilterToArtistId)
                          where (request.FilterMinimumRating == null || a.Rating >= request.FilterMinimumRating.Value)
                          where (request.FilterValue == "" || (a.Name.Contains(request.FilterValue) || a.SortName.Contains(request.FilterValue) || a.AlternateNames.Contains(request.FilterValue)))
                          where (!request.FilterFavoriteOnly || favoriteArtistIds.Contains(a.Id))
                          select new ArtistList
                          {
                              DatabaseId = a.Id,
                              Id = a.RoadieId,
                              Artist = new DataToken
                              {
                                  Text = a.Name,
                                  Value = a.RoadieId.ToString()
                              },
                              Thumbnail = this.MakeArtistThumbnailImage(a.RoadieId),
                              Rating = a.Rating,
                              CreatedDate = a.CreatedDate,
                              LastUpdated = a.LastUpdated,
                              LastPlayed = a.LastPlayed,
                              PlayedCount = a.PlayedCount,
                              ReleaseCount = a.ReleaseCount,
                              TrackCount = a.TrackCount,
                              SortName = a.SortName
                          }).Distinct();

            ArtistList[] rows = null;
            var rowCount = result.Count();
            if (doRandomize ?? false)
            {

                var randomLimit = roadieUser?.RandomReleaseLimit ?? 100;
                request.Limit = request.LimitValue > randomLimit ? randomLimit : request.LimitValue;
                rows = result.OrderBy(x => Guid.NewGuid()).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
            }
            else
            {
                string sortBy = "Id";
                if (request.ActionValue == User.ActionKeyUserRated)
                {
                    sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "Rating", "DESC" }, { "Artist.Text", "ASC" } }) : request.OrderValue(null);
                }
                else
                {
                    sortBy = request.OrderValue(new Dictionary<string, string> { { "SortName", "ASC" }, { "Artist.Text", "ASC" } });
                }
                rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
            }
            if (rows.Any() && roadieUser != null)
            {
                foreach (var userArtistRating in this.GetUser(roadieUser.UserId).ArtistRatings.Where(x => rows.Select(r => r.DatabaseId).Contains(x.ArtistId)))
                {
                    var row = rows.FirstOrDefault(x => x.DatabaseId == userArtistRating.ArtistId);
                    if (row != null)
                    {
                        row.UserRating = new UserArtist
                        {
                            IsDisliked = userArtistRating.IsDisliked ?? false,
                            IsFavorite = userArtistRating.IsFavorite ?? false,
                            Rating = userArtistRating.Rating,
                            RatedDate = userArtistRating.LastUpdated ?? userArtistRating.CreatedDate
                        };
                    }
                }
            }
            sw.Stop();
            return new Library.Models.Pagination.PagedResult<ArtistList>
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