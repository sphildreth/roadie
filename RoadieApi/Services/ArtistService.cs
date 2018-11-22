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
#pragma warning disable 1998

    public class ArtistService : ServiceBase, IArtistService
    {
        private ICollectionService CollectionService { get; } = null;
        private IPlaylistService PlaylistService { get; } = null;

        public ArtistService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext context,
                             ICacheManager cacheManager,
                             ILogger<ArtistService> logger,
                             ICollectionService collectionService,
                             IPlaylistService playlistService)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
            this.CollectionService = collectionService;
            this.PlaylistService = playlistService;
        }

        public async Task<OperationResult<Artist>> ById(User roadieUser, Guid id, IEnumerable<string> includes)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = string.Format("urn:artist_by_id_operation:{0}:{1}", id, includes == null ? "0" : string.Join("|", includes));
            var result = await this.CacheManager.GetAsync<OperationResult<Artist>>(cacheKey, async () =>
            {
                return await this.ArtistByIdAction(id, includes);
            }, data.Artist.CacheRegionUrn(id));
            if (result?.Data != null && roadieUser != null)
            {
                var artist = this.GetArtist(id);
                result.Data.UserBookmark = this.GetUserBookmarks(roadieUser).FirstOrDefault(x => x.Type == BookmarkType.Artist && x.Bookmark.Value == artist.RoadieId.ToString());
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
            }
            sw.Stop();
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
            var sw = Stopwatch.StartNew();
            sw.Start();

            var artist = this.GetArtist(id);

            if (artist == null)
            {
                return new OperationResult<Artist>(true, string.Format("Artist Not Found [{0}]", id));
            }
            var result = artist.Adapt<Artist>();
            result.Thumbnail = base.MakeArtistThumbnailImage(id);
            result.Genres = artist.Genres.Select(x => new DataToken { Text = x.Genre.Name, Value = x.Genre.RoadieId.ToString() });
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
                                    DataToken trackArtist = null;
                                    if (t.ArtistId.HasValue)
                                    {
                                        var ta = this.DbContext.Artists.FirstOrDefault(x => x.Id == t.ArtistId.Value);
                                        if (ta != null)
                                        {
                                            trackArtist = new DataToken
                                            {
                                                Text = ta.Name,
                                                Value = ta.RoadieId.ToString()
                                            };
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
                        ReleaseCount = this.DbContext.Releases.Count(x => x.ArtistId == artist.Id),
                        ReleaseMediaCount = (from r in this.DbContext.Releases
                                             join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                             where r.ArtistId == artist.Id
                                             select rm.Id).Count(),
                        TrackTime = validCartistTracks.Any() ? TimeSpan.FromSeconds(Math.Floor((double)trackTime / 1000)).ToString(@"dd\:hh\:mm\:ss") : "--:--",
                        TrackCount = validCartistTracks.Count(),
                        TrackPlayedCount = (from t in artistTracks
                                            join ut in this.DbContext.UserTracks on t.Id equals ut.TrackId
                                            select ut.PlayedCount).Sum() ?? 0
                    };
                }
                if (includes.Contains("images"))
                {
                    result.Images = this.DbContext.Images.Where(x => x.ArtistId == artist.Id).Select(x => MakeImage(x.RoadieId, this.Configuration.LargeImageSize.Width, this.Configuration.LargeImageSize.Height)).ToArray();
                }
                if (includes.Contains("associatedartists"))
                {
                    result.AssociatedWithArtists = (from aa in this.DbContext.ArtistAssociations
                                                    join a in this.DbContext.Artists on aa.AssociatedArtistId equals a.Id
                                                    where aa.ArtistId == artist.Id
                                                    orderby a.Name
                                                    select new DataToken
                                                    {
                                                        Text = a.Name,
                                                        Value = a.RoadieId.ToString()
                                                    });

                    result.AssociatedArtists = (from aa in this.DbContext.ArtistAssociations
                                                join a in this.DbContext.Artists on aa.ArtistId equals a.Id
                                                where aa.AssociatedArtistId == artist.Id
                                                orderby a.Name
                                                select new DataToken
                                                {
                                                    Text = a.Name,
                                                    Value = a.RoadieId.ToString()
                                                });
                }
                if (includes.Contains("collections"))
                {
                    var r = await this.CollectionService.List(roadieUser: null,
                                                              request: new PagedRequest(), artistId: artist.RoadieId);
                    if (r.IsSuccess)
                    {
                        result.CollectionsWithArtistReleases = r.Rows.ToArray();
                    }
                }
                if (includes.Contains("playlists"))
                {
                    var r = await this.PlaylistService.List(request: new PagedRequest(), artistId: artist.RoadieId);
                    if (r.IsSuccess)
                    {
                        result.PlaylistsWithArtistReleases = r.Rows.ToArray();
                    }
                }
                if (includes.Contains("contributions"))
                {
                    result.ArtistContributionReleases = (from t in this.DbContext.Tracks
                                                         join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                         join r in this.DbContext.Releases on rm.ReleaseId equals r.Id
                                                         where t.ArtistId == artist.Id
                                                         select new ReleaseList
                                                         {
                                                             Release = new DataToken
                                                             {
                                                                 Text = r.Title,
                                                                 Value = r.RoadieId.ToString()
                                                             },
                                                             Artist = new DataToken
                                                             {
                                                                 Value = r.Artist.RoadieId.ToString(),
                                                                 Text = r.Artist.Name
                                                             },
                                                             ArtistThumbnail = MakeArtistThumbnailImage(r.Artist.RoadieId),
                                                             Rating = r.Rating,
                                                             ReleasePlayUrl = $"{ this.HttpContext.BaseUrl }/play/release/{ r.RoadieId}",
                                                             LibraryStatus = r.LibraryStatus ?? LibraryStatus.Incomplete,
                                                             ReleaseDateDateTime = r.ReleaseDate,
                                                             TrackCount = r.TrackCount,
                                                             CreatedDate = r.CreatedDate,
                                                             LastUpdated = r.LastUpdated,
                                                             TrackPlayedCount = (from ut in this.DbContext.UserTracks
                                                                                 join t in this.DbContext.Tracks on ut.TrackId equals t.Id
                                                                                 join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                                                 join rl in this.DbContext.Releases on rm.ReleaseId equals rl.Id
                                                                                 where rl.Id == r.Id
                                                                                 select ut.PlayedCount ?? 0).Sum(),
                                                             Thumbnail = MakeReleaseThumbnailImage(r.RoadieId)
                                                         }).ToArray().GroupBy(x => x.Release.Value).Select(x => x.First()).OrderBy(x => x.Release.Text).ToArray();
                    result.ArtistContributionReleases = result.ArtistContributionReleases.Any() ? result.ArtistContributionReleases : null;
                }
                if (includes.Contains("labels"))
                {
                    result.ArtistLabels = (from l in this.DbContext.Labels
                                           let releaseCount = (from lbb in this.DbContext.Labels
                                                               join rlll in this.DbContext.ReleaseLabels on lbb.Id equals rlll.LabelId into rlddd
                                                               from rlll in rlddd.DefaultIfEmpty()
                                                               join rrr in this.DbContext.Releases on rlll.ReleaseId equals rrr.Id
                                                               where lbb.Id == l.Id
                                                               select rrr.Id).Count()
                                           let trackCount = (from lbtc in this.DbContext.Labels
                                                             join rlltc in this.DbContext.ReleaseLabels on lbtc.Id equals rlltc.LabelId into rlddtc
                                                             from rlltc in rlddtc.DefaultIfEmpty()
                                                             join rrtc in this.DbContext.Releases on rlltc.ReleaseId equals rrtc.Id
                                                             join rmtc in this.DbContext.ReleaseMedias on rrtc.Id equals rmtc.ReleaseId
                                                             join tttc in this.DbContext.Tracks on rmtc.Id equals tttc.ReleaseMediaId
                                                             where lbtc.Id == l.Id
                                                             select tttc.Id).Count()
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
                                               ReleaseCount = releaseCount,
                                               TrackCount = trackCount,
                                               Thumbnail = MakeLabelThumbnailImage(l.RoadieId)
                                           }).ToArray().GroupBy(x => x.Label.Value).Select(x => x.First()).OrderBy(x => x.SortName).ThenBy(x => x.Label.Text).ToArray();
                    result.ArtistLabels = result.ArtistLabels.Any() ? result.ArtistLabels : null;
                }
            }
            sw.Stop();
            return new OperationResult<Artist>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<Library.Models.Pagination.PagedResult<ArtistList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false)
        {
            var sw = new Stopwatch();
            sw.Start();

            int[] favoriteArtistIds = new int[0];
            if(request.FilterFavoriteOnly)
            {
                favoriteArtistIds = (from a in this.DbContext.Artists
                                     join ua in this.DbContext.UserArtists on a.Id equals ua.ArtistId
                                     where ua.IsFavorite ?? false
                                     select a.Id
                                     ).ToArray();
            }

            var result = (from a in this.DbContext.Artists
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
                              ArtistPlayedCount = 0,
                              ArtistReleaseCount = 0,
                              ArtistTrackCount = 0,
                              SortName = a.SortName
                          }).Distinct();

            ArtistList[] rows = null;
            var rowCount = result.Count();
            if (doRandomize ?? false)
            {
                request.Limit = request.LimitValue > roadieUser.RandomReleaseLimit ? roadieUser.RandomReleaseLimit : request.LimitValue;
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
                    sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "SortName", "ASC" }, { "Artist.Text", "ASC" } }) : request.OrderValue(null);
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
                            Rating = userArtistRating.Rating
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