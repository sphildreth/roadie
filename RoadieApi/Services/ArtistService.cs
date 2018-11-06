using Microsoft.Extensions.Logging;
using Mapster;
using Mapster.Adapters;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using data = Roadie.Library.Data;
using System.Linq;
using Roadie.Library.Enums;
using Roadie.Library.Models.Users;
using Roadie.Library.Models.Releases;
using Microsoft.EntityFrameworkCore;
using Roadie.Library.Models.Statistics;
using Roadie.Library.Extensions;
using Roadie.Library.Utility;

namespace Roadie.Api.Services
{
    public class ArtistService : ServiceBase, IArtistService
    {
        public ArtistService(IRoadieSettings configuration, 
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext context,
                             ICacheManager cacheManager,
                             ILogger<ArtistService> logger)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
        }

        public async Task<OperationResult<bool>> AddArtist(Artist artist)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<Artist>> ArtistById(User roadieUser, Guid id, IEnumerable<string> includes)
        {
            roadieUser = roadieUser ?? new User();
            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheRegion = (new data.Artist { RoadieId = id }).CacheRegion;
            var cacheKey = string.Format("urn:artist_result_model:{0}:{1}", id, includes == null ? "0" : string.Join("|", includes));
            var resultInCache = this._cacheManager.Get<OperationResult<Artist>>(cacheKey, cacheRegion);
            if (resultInCache != null)
            {
                sw.Stop();
                resultInCache.OperationTime = sw.ElapsedMilliseconds;
                return resultInCache;
            }

                var artist = this.DbContext.Artists
                                           .Include(x => x.Genres)
                                           .Include("Genres.Genre")
                                           .FirstOrDefault(x => x.RoadieId == id);
                if (artist == null)
                {
                    sw.Stop();
                    return new OperationResult<Artist>(string.Format("Artist Not Found [{0}]", id))
                    {
                        OperationTime = sw.ElapsedMilliseconds
                    };
                }
                var result = artist.Adapt<Artist>();
                result.Thumbnail = base.MakeArtistThumbnailImage(id);
                result.UserBookmarkId = (from b in this.DbContext.Bookmarks
                                           where b.UserId == roadieUser.Id
                                           where b.BookmarkTargetId == artist.Id
                                           where b.BookmarkType == BookmarkType.Artist
                                           select b.RoadieId).FirstOrDefault();
                result.Genres = artist.Genres.Select(x => new DataToken { Text = x.Genre.Name, Value = x.Genre.RoadieId });
                if (includes != null && includes.Any())
                {
                    if (includes.Contains("releases"))
                    {
                    var dtoReleases = new List<ReleaseList>();
                        foreach (var release in this.DbContext.Releases.Where(x => x.ArtistId == artist.Id).ToArray()) // this.DbContext.Releases.Include("Medias").Include("Medias.Tracks").Where(x => x.ArtistId == artist.Id).ToArray())
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
                        result.Images = this.DbContext.Images.Where(x => x.ArtistId == artist.Id).Select(x => MakeImage(x.RoadieId, this.Configuration.LargeThumbnails.Width, this.Configuration.LargeThumbnails.Height)).ToArray();
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
                                                            Value = a.RoadieId
                                                        });

                        result.AssociatedArtists = (from aa in this.DbContext.ArtistAssociations
                                                    join a in this.DbContext.Artists on aa.ArtistId equals a.Id
                                                    where aa.AssociatedArtistId == artist.Id
                                                    orderby a.Name
                                                    select new DataToken
                                                    {
                                                        Text = a.Name,
                                                        Value = a.RoadieId

                                                    });
                    }
                    // TODO once other services are in place
                    //if (includes.Contains("collections"))
                    //{
                    //    var r = this.CollectionModule.CollectionList(request: new PagedRequestModel(), artistId: artist.roadieId);
                    //    if (r.IsSuccess)
                    //    {
                    //        result.CollectionsWithArtistReleases = r.Rows.GroupBy(x => x.roadieId).Select(x => x.First()).OrderBy(x => x.Name).ToArray();
                    //    }
                    //}
                    //if (includes.Contains("playlists"))
                    //{
                    //    var r = this.PlaylistModule.PlaylistList(request: new PagedRequestModel(), artistId: artist.roadieId);
                    //    if (r.IsSuccess)
                    //    {
                    //        result.PlaylistsWithArtistReleases = r.Rows.GroupBy(x => x.roadieId).Select(x => x.First()).OrderBy(x => x.Name).ToArray();
                    //    }
                    //}
                    if (includes.Contains("contributions"))
                    {
                        result.ArtistContributionReleases = (from t in this.DbContext.Tracks
                                                             join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                             join r in this.DbContext.Releases on rm.ReleaseId equals r.Id
                                                             join userR in this.DbContext.UserReleases on r.Id equals userR.ReleaseId into userRas
                                                             from userR in userRas.DefaultIfEmpty()
                                                             where t.ArtistId == artist.Id
                                                             where (userR == null || userR.Id == roadieUser.Id)
                                                             select new ReleaseList
                                                             {
                                                                 Release = new DataToken {
                                                                     Text = r.Title,
                                                                     Value = r.RoadieId
                                                                 },
                                                                 ArtistId = r.Artist.RoadieId,
                                                                 ArtistName = r.Artist.Name,
                                                                 ArtistThumbnail = MakeArtistThumbnailImage(r.Artist.RoadieId),
                                                                 Rating = userR != null ? userR.Rating : r.Rating,
                                                                 ReleasePlayUrl = $"{ this.HttpContext.BaseUrl }/play/release/{ r.RoadieId}",
                                                                 UserRating = userR != null ? (short?)userR.Rating : null,
                                                                 LibraryStatus = r.LibraryStatus ?? LibraryStatus.Incomplete,
                                                                 ReleaseDateDateTime = r.ReleaseDate,
                                                                 TrackCount = r.TrackCount,
                                                                 CreatedDateTime = r.CreatedDate,
                                                                 LastUpdatedDateTime = r.LastUpdated,
                                                                 TrackPlayedCount = (from ut in this.DbContext.UserTracks
                                                                                     join t in this.DbContext.Tracks on ut.TrackId equals t.Id
                                                                                     join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                                                     join rl in this.DbContext.Releases on rm.ReleaseId equals rl.Id
                                                                                     where rl.Id == r.Id
                                                                                     select ut.PlayedCount ?? 0).Sum(),
                                                                 Thumbnail = MakeReleaseThumbnailImage(r.RoadieId)
                                                             }).ToArray().GroupBy(x => x.Release.Value).Select(x => x.First()).OrderBy(x => x.Release.Text).ToArray();
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
                                                   Label = new DataToken
                                                   {
                                                       Text = l.Name,
                                                       Value = l.RoadieId,
                                                   },
                                                   SortName = l.SortName,
                                                   CreatedDateTime = l.CreatedDate,
                                                   LastUpdatedDateTime = l.LastUpdated,
                                                   ReleaseCount = releaseCount,
                                                   TrackCount = trackCount,
                                                   Thumbnail = MakeLabelThumbnailImage(l.RoadieId)
                                               }).ToArray().GroupBy(x => x.Label.Value).Select(x => x.First()).OrderBy(x => x.SortName).ThenBy(x => x.Label.Text).ToArray();
                    }
                }

                if (roadieUser != null)
                {
                    var userArtist = this.DbContext.UserArtists.FirstOrDefault(x => x.ArtistId == artist.Id && x.UserId == roadieUser.Id);
                    if (userArtist != null)
                    {
                        result.UserArtist = userArtist.Adapt<UserArtist>();                        
                    }
                }
                sw.Stop();
                resultInCache = new OperationResult<Artist>
                {
                    Data = result,
                    IsSuccess = result != null,
                    OperationTime = sw.ElapsedMilliseconds
                };
                this._cacheManager.Add(cacheKey, resultInCache, cacheRegion);
                return resultInCache;
            

        }

        public async Task<OperationResult<Artist>> ArtistByName(string name, IEnumerable<string> includes)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> DeleteArtist(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> DeleteArtistReleases(Guid roadieId)
        {
            throw new NotImplementedException();
        }

        public async Task<PagedResult<ArtistList>> List(PagedRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> MergeArtists(Guid artistId, Guid mergeInfoArtistId)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> MergeReleases(Guid artistId, string releaseIdToMerge, string releaseIdToMergeInto, bool addAsMedia)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> RefreshArtistMetaData(Guid artistId)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> RescanArtist(Guid artistId)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> SetImage(Guid id, byte[] imageBytes)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> SetImageByImageId(Guid artistId, Guid imageId)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> SetImageViaUrl(Guid id, string imageUrl)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> SetUserRating(Guid artistId, Guid userId, short rating)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> ToggleUserDislikeArtist(Guid artistId, Guid UserId, bool dislike)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> ToggleUserFavoriteArtist(Guid artistId, Guid UserId, bool favorite)
        {
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> UpdateArtist(Artist ea)
        {
            throw new NotImplementedException();
        }
    }
}