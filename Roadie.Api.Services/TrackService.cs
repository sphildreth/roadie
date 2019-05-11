using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Imaging;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Releases;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public class TrackService : ServiceBase, ITrackService
    {
        private IBookmarkService BookmarkService { get; } = null;
        private IAdminService AdminService { get; }

        public TrackService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext dbContext,
                             ICacheManager cacheManager,
                             ILogger<TrackService> logger,
                             IBookmarkService bookmarkService,
                             IAdminService adminService)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
            this.BookmarkService = bookmarkService;
            this.AdminService = adminService;
        }

        public static long DetermineByteEndFromHeaders(IHeaderDictionary headers, long fileLength)
        {
            var defaultFileLength = fileLength - 1;
            if (headers == null || !headers.Any(x => x.Key == "Range"))
            {
                return defaultFileLength;
            }
            long? result = null;
            var rangeHeader = headers["Range"];
            string rangeEnd = null;
            var rangeBegin = rangeHeader.FirstOrDefault();
            if (!string.IsNullOrEmpty(rangeBegin))
            {
                //bytes=0-
                rangeBegin = rangeBegin.Replace("bytes=", "");
                var parts = rangeBegin.Split('-');
                rangeBegin = parts[0];
                if (parts.Length > 1)
                {
                    rangeEnd = parts[1];
                }
                if (!string.IsNullOrEmpty(rangeEnd))
                {
                    result = long.TryParse(rangeEnd, out long outValue) ? (int?)outValue : null;
                }
            }
            return result ?? defaultFileLength;
        }

        public static long DetermineByteStartFromHeaders(IHeaderDictionary headers)
        {
            if (headers == null || !headers.Any(x => x.Key == "Range"))
            {
                return 0;
            }
            long result = 0;
            var rangeHeader = headers["Range"];
            var rangeBegin = rangeHeader.FirstOrDefault();
            if (!string.IsNullOrEmpty(rangeBegin))
            {
                //bytes=0-
                rangeBegin = rangeBegin.Replace("bytes=", "");
                var parts = rangeBegin.Split('-');
                rangeBegin = parts[0];
                if (!string.IsNullOrEmpty(rangeBegin))
                {
                    long.TryParse(rangeBegin, out result);
                }
            }
            return result;
        }

        public async Task<OperationResult<Track>> ById(User roadieUser, Guid id, IEnumerable<string> includes)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = string.Format("urn:track_by_id_operation:{0}:{1}", id, includes == null ? "0" : string.Join("|", includes));
            var result = await this.CacheManager.GetAsync<OperationResult<Track>>(cacheKey, async () =>
            {
                return await this.TrackByIdAction(id, includes);
            }, data.Track.CacheRegionUrn(id));
            if (result?.Data != null && roadieUser != null)
            {
                var user = this.GetUser(roadieUser.UserId);
                var track = this.GetTrack(id);
                result.Data.TrackPlayUrl = this.MakeTrackPlayUrl(user, track.Id, track.RoadieId);
                var userBookmarkResult = await this.BookmarkService.List(roadieUser, new PagedRequest(), false, BookmarkType.Track);
                if (userBookmarkResult.IsSuccess)
                {
                    result.Data.UserBookmarked = userBookmarkResult?.Rows?.FirstOrDefault(x => x.Bookmark.Value == track.RoadieId.ToString()) != null;
                }
                var userTrack = this.DbContext.UserTracks.FirstOrDefault(x => x.TrackId == track.Id && x.UserId == roadieUser.Id);
                if (userTrack != null)
                {
                    result.Data.UserRating = new UserTrack
                    {
                        Rating = userTrack.Rating,
                        IsDisliked = userTrack.IsDisliked ?? false,
                        IsFavorite = userTrack.IsFavorite ?? false,
                        LastPlayed = userTrack.LastPlayed,
                        PlayedCount = userTrack.PlayedCount
                    };
                }
            }
            sw.Stop();
            return new OperationResult<Track>(result.Messages)
            {
                Data = result?.Data,
                Errors = result?.Errors,
                IsNotFoundResult = result?.IsNotFoundResult ?? false,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public Task<Library.Models.Pagination.PagedResult<TrackList>> List(PagedRequest request, User roadieUser, bool? doRandomize = false, Guid? releaseId = null)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                int? rowCount = null;

                if (!string.IsNullOrEmpty(request.Sort))
                {
                    request.Sort = request.Sort.Replace("Release.Text", "Release.Release.Text");
                }

                IQueryable<int> favoriteTrackIds = (new int[0]).AsQueryable();
                if (request.FilterFavoriteOnly)
                {
                    favoriteTrackIds = (from t in this.DbContext.Tracks
                                        join ut in this.DbContext.UserTracks on t.Id equals ut.TrackId
                                        where ut.UserId == roadieUser.Id
                                        where ut.IsFavorite ?? false
                                        select t.Id
                                        );
                }
                Dictionary<int, int> playListTrackPositions = new Dictionary<int, int>();
                int[] playlistTrackIds = new int[0];
                if (request.FilterToPlaylistId.HasValue)
                {
                    var playlistTrackInfos = (from plt in this.DbContext.PlaylistTracks
                                              join p in this.DbContext.Playlists on plt.PlayListId equals p.Id
                                              join t in this.DbContext.Tracks on plt.TrackId equals t.Id
                                              where p.RoadieId == request.FilterToPlaylistId.Value
                                              orderby plt.ListNumber
                                              select new
                                              {
                                                  plt.ListNumber,
                                                  t.Id
                                              });

                    rowCount = playlistTrackInfos.Count();
                    playListTrackPositions = playlistTrackInfos.Skip(request.SkipValue).Take(request.LimitValue).ToDictionary(x => x.Id, x => x.ListNumber);
                    playlistTrackIds = playListTrackPositions.Select(x => x.Key).ToArray();
                    request.Sort = "TrackNumber";
                    request.Order = "ASC";
                    request.Page = 1; // Set back to first or it skips already paged tracks for playlist
                    request.SkipValue = 0;
                }

                int[] collectionTrackIds = new int[0];
                if (request.FilterToCollectionId.HasValue)
                {
                    request.Limit = roadieUser?.PlayerTrackLimit ?? 50;

                    collectionTrackIds = (from cr in this.DbContext.CollectionReleases
                                          join c in this.DbContext.Collections on cr.CollectionId equals c.Id
                                          join r in this.DbContext.Releases on cr.ReleaseId equals r.Id
                                          join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                          join t in this.DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                          where c.RoadieId == request.FilterToCollectionId.Value
                                          orderby cr.ListNumber, rm.MediaNumber, t.TrackNumber
                                          select t.Id).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
                }

                int[] topTrackids = new int[0];
                if (request.FilterTopPlayedOnly)
                {
                    // Get request number of top played songs for artist
                    topTrackids = (from t in this.DbContext.Tracks
                                   join ut in this.DbContext.UserTracks on t.Id equals ut.TrackId
                                   join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                   join r in this.DbContext.Releases on rm.ReleaseId equals r.Id
                                   join a in this.DbContext.Artists on r.ArtistId equals a.Id
                                   where a.RoadieId == request.FilterToArtistId
                                   orderby ut.PlayedCount descending
                                   select t.Id
                                   ).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
                }
                int[] randomTrackIds = null;
                if (doRandomize ?? false)
                {
                    request.Limit = roadieUser?.RandomReleaseLimit ?? 50;
                    var userId = roadieUser?.Id ?? -1;

                    if (!request.FilterRatedOnly && !request.FilterFavoriteOnly)
                    {
                        var sql = @"SELECT t.id
                                        FROM `track` t 
                                        JOIN `releasemedia` rm on (t.releaseMediaId = rm.id)
                                        WHERE t.Hash IS NOT NULL 
                                        AND t.id NOT IN (SELECT ut.trackId
                                                           FROM `usertrack` ut
                                                           WHERE ut.userId = {0}
                                                           AND ut.isDisliked = 1)                  
                                        AND rm.releaseId in (select distinct r.id
                                            FROM `release` r
                                            WHERE r.id NOT IN (SELECT ur.releaseId
                                                               FROM `userrelease` ur
                                                               WHERE ur.userId = {0}
                                                               AND ur.isDisliked = 1)
                                            AND r.artistId IN (select DISTINCT a.id
                                                                 FROM `artist` a 
                                                                 WHERE a.id NOT IN (select ua.artistId
							                                        FROM `userartist` ua 
							                                        where ua.userId = {0}
							                                        AND ua.isDisliked = 1)
                                                                 ORDER BY RAND())
                                            ORDER BY RAND())
                                        ORDER BY RAND()	
                                        LIMIT {1}";
                        randomTrackIds = this.DbContext.Tracks.FromSql(sql, userId, request.Limit).Select(x => x.Id).ToArray();
                    }
                    if (request.FilterRatedOnly && !request.FilterFavoriteOnly)
                    {
                        var sql = @"SELECT t.id
                                        FROM `track` t 
                                        JOIN `releasemedia` rm on (t.releaseMediaId = rm.id)
                                        WHERE t.Hash IS NOT NULL 
                                        AND t.rating > 0
                                        AND t.id NOT IN (SELECT ut.trackId
                                                           FROM `usertrack` ut
                                                           WHERE ut.userId = {0}
                                                           AND ut.isDisliked = 1)                  
                                        AND rm.releaseId in (select distinct r.id
                                            FROM `release` r
                                            WHERE r.id NOT IN (SELECT ur.releaseId
                                                               FROM `userrelease` ur
                                                               WHERE ur.userId = {0}
                                                               AND ur.isDisliked = 1)
                                            AND r.artistId IN (select DISTINCT a.id
                                                                 FROM `artist` a 
                                                                 WHERE a.id NOT IN (select ua.artistId
							                                        FROM `userartist` ua 
							                                        where ua.userId = {0}
							                                        AND ua.isDisliked = 1)
                                                                 ORDER BY RAND())
                                            ORDER BY RAND())
                                        ORDER BY RAND()	
                                        LIMIT {1}";
                        randomTrackIds = this.DbContext.Tracks.FromSql(sql, userId, request.LimitValue).Select(x => x.Id).ToArray();
                    }
                    if (request.FilterFavoriteOnly)
                    {
                        rowCount = favoriteTrackIds.Count();
                    }
                    else
                    {
                        rowCount = this.DbContext.Tracks.Where(x => x.Hash != null).Count();
                    }
                }
                Guid?[] filterToTrackIds = null;
                if (request.FilterToTrackId.HasValue || request.FilterToTrackIds != null)
                {
                    var f = new List<Guid?>();
                    if (request.FilterToTrackId.HasValue)
                    {
                        f.Add(request.FilterToTrackId);
                    }
                    if (request.FilterToTrackIds != null)
                    {
                        foreach (var ft in request.FilterToTrackIds)
                        {
                            if (!f.Contains(ft))
                            {
                                f.Add(ft);
                            }
                        }
                    }
                    filterToTrackIds = f.ToArray();
                }
                var normalizedFilterValue = !string.IsNullOrEmpty(request.FilterValue) ? request.FilterValue.ToAlphanumericName() : null;

                var isEqualFilter = false;
                if (!string.IsNullOrEmpty(request.FilterValue))
                {
                    var filter = request.FilterValue;
                    // if filter string is wrapped in quotes then is an exact not like search, e.g. "Diana Ross" should not return "Diana Ross & The Supremes"
                    if (filter.StartsWith('"') && filter.EndsWith('"'))
                    {
                        isEqualFilter = true;
                        request.Filter = filter.Substring(1, filter.Length - 2);
                    }
                }

                // Did this for performance against the Track table, with just * selcts the table scans are too much of a performance hit.
                var resultQuery = (from t in this.DbContext.Tracks
                                   join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                   join r in this.DbContext.Releases on rm.ReleaseId equals r.Id
                                   join releaseArtist in this.DbContext.Artists on r.ArtistId equals releaseArtist.Id
                                   join trackArtist in this.DbContext.Artists on t.ArtistId equals trackArtist.Id into tas
                                   from trackArtist in tas.DefaultIfEmpty()
                                   where (t.Hash != null)
                                   where (releaseId == null || (releaseId != null && r.RoadieId == releaseId))
                                   where (filterToTrackIds == null || filterToTrackIds.Contains(t.RoadieId))
                                   where (request.FilterMinimumRating == null || t.Rating >= request.FilterMinimumRating.Value)
                                   where (request.FilterValue == "" || (t.Title.Contains(request.FilterValue) || 
                                                                        t.AlternateNames.Contains(request.FilterValue) || 
                                                                        t.AlternateNames.Contains(normalizedFilterValue)) ||
                                                                        t.PartTitles.Contains(request.FilterValue))
                                   where (!isEqualFilter || (t.Title.Equals(request.FilterValue) ||
                                                            t.AlternateNames.Equals(request.FilterValue) ||
                                                            t.AlternateNames.Equals(normalizedFilterValue)) ||
                                                            t.PartTitles.Equals(request.FilterValue))
                                   where (!request.FilterFavoriteOnly || favoriteTrackIds.Contains(t.Id))
                                   where (request.FilterToPlaylistId == null || playlistTrackIds.Contains(t.Id))
                                   where (!request.FilterTopPlayedOnly || topTrackids.Contains(t.Id))
                                   where (randomTrackIds == null || randomTrackIds.Contains(t.Id))
                                   where (request.FilterToArtistId == null || request.FilterToArtistId != null && ((t.TrackArtist != null && t.TrackArtist.RoadieId == request.FilterToArtistId) || r.Artist.RoadieId == request.FilterToArtistId))
                                   where (!request.IsHistoryRequest || t.PlayedCount > 0)
                                   where (request.FilterToCollectionId == null || collectionTrackIds.Contains(t.Id))
                                   select new
                                   {
                                       ti = new
                                       {
                                           t.Id,
                                           t.RoadieId,
                                           t.CreatedDate,
                                           t.LastUpdated,
                                           t.LastPlayed,
                                           t.Duration,
                                           t.FileSize,
                                           t.PlayedCount,
                                           t.PartTitles,
                                           t.Rating,
                                           t.Tags,
                                           t.TrackNumber,
                                           t.Title
                                       },
                                       rmi = new
                                       {
                                           rm.MediaNumber
                                       },
                                       rl = new ReleaseList
                                       {
                                           DatabaseId = r.Id,
                                           Id = r.RoadieId,
                                           Artist = new DataToken
                                           {
                                               Value = releaseArtist.RoadieId.ToString(),
                                               Text = releaseArtist.Name
                                           },
                                           Release = new DataToken
                                           {
                                               Text = r.Title,
                                               Value = r.RoadieId.ToString()
                                           },
                                           ArtistThumbnail = this.MakeArtistThumbnailImage(releaseArtist.RoadieId),
                                           CreatedDate = r.CreatedDate,
                                           Duration = r.Duration,
                                           LastPlayed = r.LastPlayed,
                                           LastUpdated = r.LastUpdated,
                                           LibraryStatus = r.LibraryStatus,
                                           MediaCount = r.MediaCount,
                                           Rating = r.Rating,
                                           Rank = r.Rank,
                                           ReleaseDateDateTime = r.ReleaseDate,
                                           ReleasePlayUrl = $"{ this.HttpContext.BaseUrl }/play/release/{ r.RoadieId}",
                                           Status = r.Status,
                                           Thumbnail = this.MakeReleaseThumbnailImage(r.RoadieId),
                                           TrackCount = r.TrackCount,
                                           TrackPlayedCount = r.PlayedCount
                                       },
                                       ta = trackArtist == null ? null : new ArtistList
                                       {
                                           DatabaseId = trackArtist.Id,
                                           Id = trackArtist.RoadieId,
                                           Artist = new DataToken { Text = trackArtist.Name, Value = trackArtist.RoadieId.ToString() },
                                           Rating = trackArtist.Rating,
                                           Rank = trackArtist.Rank,
                                           CreatedDate = trackArtist.CreatedDate,
                                           LastUpdated = trackArtist.LastUpdated,
                                           LastPlayed = trackArtist.LastPlayed,
                                           PlayedCount = trackArtist.PlayedCount,
                                           ReleaseCount = trackArtist.ReleaseCount,
                                           TrackCount = trackArtist.TrackCount,
                                           SortName = trackArtist.SortName,
                                           Thumbnail = this.MakeArtistThumbnailImage(trackArtist.RoadieId)
                                       },
                                       ra = new ArtistList
                                       {
                                           DatabaseId = releaseArtist.Id,
                                           Id = releaseArtist.RoadieId,
                                           Artist = new DataToken { Text = releaseArtist.Name, Value = releaseArtist.RoadieId.ToString() },
                                           Rating = releaseArtist.Rating,
                                           Rank = releaseArtist.Rank,
                                           CreatedDate = releaseArtist.CreatedDate,
                                           LastUpdated = releaseArtist.LastUpdated,
                                           LastPlayed = releaseArtist.LastPlayed,
                                           PlayedCount = releaseArtist.PlayedCount,
                                           ReleaseCount = releaseArtist.ReleaseCount,
                                           TrackCount = releaseArtist.TrackCount,
                                           SortName = releaseArtist.SortName,
                                           Thumbnail = this.MakeArtistThumbnailImage(releaseArtist.RoadieId)
                                       }
                                   });

                if (!string.IsNullOrEmpty(request.FilterValue))
                {
                    if (request.FilterValue.StartsWith("#"))
                    {
                        // Find any releases by tags
                        var tagValue = request.FilterValue.Replace("#", "");
                        resultQuery = resultQuery.Where(x => x.ti.Tags != null && x.ti.Tags.Contains(tagValue));
                    }
                }
                var user = this.GetUser(roadieUser.UserId);
                var result = resultQuery.Select(x =>
                              new TrackList
                              {
                                  DatabaseId = x.ti.Id,
                                  Id = x.ti.RoadieId,
                                  Track = new DataToken
                                  {
                                      Text = x.ti.Title,
                                      Value = x.ti.RoadieId.ToString()
                                  },
                                  Artist = x.ra,
                                  CreatedDate = x.ti.CreatedDate,
                                  Duration = x.ti.Duration,
                                  FileSize = x.ti.FileSize,
                                  LastPlayed = x.ti.LastPlayed,
                                  LastUpdated = x.ti.LastUpdated,
                                  MediaNumber = x.rmi.MediaNumber,
                                  PlayedCount = x.ti.PlayedCount,
                                  PartTitles = x.ti.PartTitles,
                                  Rating = x.ti.Rating,
                                  Release = x.rl,
                                  ReleaseDate = x.rl.ReleaseDateDateTime,
                                  Thumbnail = this.MakeTrackThumbnailImage(x.ti.RoadieId),
                                  Title = x.ti.Title,
                                  TrackArtist = x.ta,
                                  TrackNumber = playListTrackPositions.ContainsKey(x.ti.Id) ? playListTrackPositions[x.ti.Id] : x.ti.TrackNumber,
                                  TrackPlayUrl = this.MakeTrackPlayUrl(user, x.ti.Id, x.ti.RoadieId)
                              });
                string sortBy = null;

                rowCount = rowCount ?? result.Count();
                TrackList[] rows = null;

                if (request.Action == User.ActionKeyUserRated)
                {
                    sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "UserTrack.Rating", "DESC" }, { "MediaNumber", "ASC" }, { "TrackNumber", "ASC" } }) : request.OrderValue(null);
                }
                else
                {
                    sortBy = string.IsNullOrEmpty(request.Sort) ? request.OrderValue(new Dictionary<string, string> { { "Release.Release.Text", "ASC" }, { "MediaNumber", "ASC" }, { "TrackNumber", "ASC" } }) : request.OrderValue(null);
                }
                if(doRandomize ?? false)
                {
                    rows = result.OrderBy(x => x.RandomSortId).Take(request.LimitValue).ToArray();
                }
                else
                {
                    rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
                }                
                if (rows.Any() && roadieUser != null)
                {
                    var rowIds = rows.Select(x => x.DatabaseId).ToArray();
                    var userTrackRatings = (from ut in this.DbContext.UserTracks
                                            where ut.UserId == roadieUser.Id
                                            where rowIds.Contains(ut.TrackId)
                                            select ut).ToArray();

                    foreach (var userTrackRating in userTrackRatings)
                    {
                        var row = rows.FirstOrDefault(x => x.DatabaseId == userTrackRating.TrackId);
                        if (row != null)
                        {
                            row.UserRating = new UserTrack
                            {
                                IsDisliked = userTrackRating.IsDisliked ?? false,
                                IsFavorite = userTrackRating.IsFavorite ?? false,
                                Rating = userTrackRating.Rating,
                                LastPlayed = userTrackRating.LastPlayed,
                                PlayedCount = userTrackRating.PlayedCount
                            };
                        }
                    }

                    var releaseIds = rows.Select(x => x.Release.DatabaseId).Distinct().ToArray();
                    var userReleaseRatings = (from ur in this.DbContext.UserReleases
                                              where releaseIds.Contains(ur.ReleaseId)
                                              select ur).ToArray();

                    foreach (var userReleaseRating in userReleaseRatings)
                    {
                        foreach (var row in rows.Where(x => x.Release.DatabaseId == userReleaseRating.ReleaseId))
                        {
                            row.Release.UserRating = userReleaseRating.Adapt<UserRelease>();
                        }
                    }

                    var artistIds = rows.Select(x => x.Artist.DatabaseId).ToArray();
                    if (artistIds != null && artistIds.Any())
                    {
                        var userArtistRatings = (from ua in this.DbContext.UserArtists
                                                 where ua.UserId == roadieUser.Id
                                                 where artistIds.Contains(ua.ArtistId)
                                                 select ua).ToArray();
                        foreach (var userArtistRating in userArtistRatings)
                        {
                            foreach (var artistTrack in rows.Where(x => x.Artist.DatabaseId == userArtistRating.ArtistId))
                            {
                                artistTrack.Artist.UserRating = userArtistRating.Adapt<UserArtist>();
                            }
                        }
                    }

                    var trackArtistIds = rows.Where(x => x.TrackArtist != null).Select(x => x.TrackArtist.DatabaseId).ToArray();
                    if (trackArtistIds != null && trackArtistIds.Any())
                    {
                        var userTrackArtistRatings = (from ua in this.DbContext.UserArtists
                                                      where ua.UserId == roadieUser.Id
                                                      where trackArtistIds.Contains(ua.ArtistId)
                                                      select ua).ToArray();
                        if (userTrackArtistRatings != null && userTrackArtistRatings.Any())
                        {
                            foreach (var userTrackArtistRating in userTrackArtistRatings)
                            {
                                foreach (var artistTrack in rows.Where(x => x.TrackArtist != null && x.TrackArtist.DatabaseId == userTrackArtistRating.ArtistId))
                                {
                                    artistTrack.Artist.UserRating = userTrackArtistRating.Adapt<UserArtist>();
                                }
                            }
                        }
                    }
                }

                if (rows.Any())
                {
                    foreach (var row in rows)
                    {
                        row.FavoriteCount = (from ut in this.DbContext.UserTracks
                                             join tr in this.DbContext.Tracks on ut.TrackId equals tr.Id
                                             where ut.TrackId == row.DatabaseId
                                             where ut.IsFavorite ?? false
                                             select ut.Id).Count();
                    }
                }

                sw.Stop();
                return Task.FromResult(new Library.Models.Pagination.PagedResult<TrackList>
                {
                    TotalCount = rowCount ?? 0,
                    CurrentPage = request.PageValue,
                    TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                    OperationTime = sw.ElapsedMilliseconds,
                    Rows = rows
                });
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Error In List, Request [{0}], User [{1}]", JsonConvert.SerializeObject(request), roadieUser);
                return Task.FromResult(new Library.Models.Pagination.PagedResult<TrackList>
                {
                    Message = "An Error has occured"
                });
            }
        }

        public async Task<OperationResult<bool>> UpdateTrack(User user, Track model)
        {
            var didChangeTrack = false;
            var didChangeThumbnail = false;
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var track = this.DbContext.Tracks
                                      .Include(x => x.ReleaseMedia)
                                      .Include(x => x.ReleaseMedia.Release)
                                      .Include(x => x.ReleaseMedia.Release.Artist)
                                      .FirstOrDefault(x => x.RoadieId == model.Id);
            if (track == null)
            {
                return new OperationResult<bool>(true, string.Format("Track Not Found [{0}]", model.Id));
            }
            try
            {
                var now = DateTime.UtcNow;
                track.IsLocked = model.IsLocked;
                track.Status = SafeParser.ToEnum<Statuses>(model.Status);
                track.Title = model.Title;
                track.AlternateNames = model.AlternateNamesList.ToDelimitedList();
                track.Rating = model.Rating;
                track.AmgId = model.AmgId;
                track.LastFMId = model.LastFMId;
                track.MusicBrainzId = model.MusicBrainzId;
                track.SpotifyId = model.SpotifyId;
                track.Tags = model.TagsList.ToDelimitedList();
                track.PartTitles = model.PartTitlesList == null || !model.PartTitlesList.Any() ? null : string.Join("\n", model.PartTitlesList);

                if (model.TrackArtistToken != null)
                {
                    var artistId = SafeParser.ToGuid(model.TrackArtistToken.Value);
                    if (artistId.HasValue)
                    {
                        var artist = this.GetArtist(artistId.Value);
                        if (artist != null)
                        {
                            track.ArtistId = artist.Id;
                        }
                    }
                }
                else
                {
                    track.ArtistId = null;
                }

                var trackImage = ImageHelper.ImageDataFromUrl(model.NewThumbnailData);
                if (trackImage != null)
                {
                    // Ensure is jpeg first
                    track.Thumbnail = ImageHelper.ConvertToJpegFormat(trackImage);

                    // Save unaltered image to cover file
                    var trackThumbnailName = track.PathToTrackThumbnail(this.Configuration, this.Configuration.LibraryFolder);
                    File.WriteAllBytes(trackThumbnailName, track.Thumbnail);

                    // Resize to store in database as thumbnail
                    track.Thumbnail = ImageHelper.ResizeImage(track.Thumbnail, this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height);
                    didChangeThumbnail = true;
                }
                track.LastUpdated = now;
                await this.DbContext.SaveChangesAsync();
                this.CacheManager.ClearRegion(track.CacheRegion);
                this.Logger.LogInformation($"UpdateTrack `{ track }` By User `{ user }`: Edited Track [{ didChangeTrack }], Uploaded new image [{ didChangeThumbnail }]");
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex);
                errors.Add(ex);
            }
            sw.Stop();

            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = !errors.Any(),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }


        /// <summary>
        /// Fast as possible check if exists and return minimum information on Track
        /// </summary>
        public OperationResult<Track> StreamCheckAndInfo(User roadieUser, Guid id)
        {
            var track = this.DbContext.Tracks.FirstOrDefault(x => x.RoadieId == id);
            if (track == null)
            {
                return new OperationResult<Track>(true, string.Format("Track Not Found [{0}]", id));
            }
            return new OperationResult<Track>()
            {
                Data = track.Adapt<Track>(),
                IsSuccess = true
            };
        }

        public async Task<OperationResult<TrackStreamInfo>> TrackStreamInfo(Guid trackId, long beginBytes, long endBytes, User roadieUser)
        {
            var track = this.DbContext.Tracks.FirstOrDefault(x => x.RoadieId == trackId);
            if (track == null)
            {
                // Not Found try recanning release 
                var release = (from r in this.DbContext.Releases
                               join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                               where rm.Id == track.ReleaseMediaId
                               select r).FirstOrDefault();
                if (!release.IsLocked ?? false)
                {
                    await this.AdminService.ScanRelease(new Library.Identity.ApplicationUser
                    {
                        Id = roadieUser.Id.Value
                    }, release.RoadieId, false, true);
                }
                track = this.DbContext.Tracks.FirstOrDefault(x => x.RoadieId == trackId);
                if(track == null)
                {
                    return new OperationResult<TrackStreamInfo>($"TrackStreamInfo: Unable To Find Track [{ trackId }]");
                }
            }
            if (!track.IsValid)
            {
                return new OperationResult<TrackStreamInfo>($"TrackStreamInfo: Invalid Track. Track Id [{trackId}], FilePath [{track.FilePath}], Filename [{track.FileName}]");
            }
            string trackPath = null;
            try
            {
                trackPath = track.PathToTrack(this.Configuration, this.Configuration.LibraryFolder);
            }
            catch (Exception ex)
            {
                return new OperationResult<TrackStreamInfo>(ex);
            }
            var trackFileInfo = new FileInfo(trackPath);
            if (!trackFileInfo.Exists)
            {
                // Not Found try recanning release 
                var release = (from r in this.DbContext.Releases
                               join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                               where rm.Id == track.ReleaseMediaId
                               select r).FirstOrDefault();
                if (!release.IsLocked ?? false)
                {
                    await this.AdminService.ScanRelease(new Library.Identity.ApplicationUser
                    {
                        Id = roadieUser.Id.Value
                    }, release.RoadieId, false, true);
                }
                track = this.DbContext.Tracks.FirstOrDefault(x => x.RoadieId == trackId);
                if (track == null)
                {
                    return new OperationResult<TrackStreamInfo>($"TrackStreamInfo: Unable To Find Track [{ trackId }]");
                }
                try
                {
                    trackPath = track.PathToTrack(this.Configuration, this.Configuration.LibraryFolder);
                }
                catch (Exception ex)
                {
                    return new OperationResult<TrackStreamInfo>(ex);
                }
                if (!trackFileInfo.Exists)
                { 
                    track.UpdateTrackMissingFile();
                    await this.DbContext.SaveChangesAsync();
                    return new OperationResult<TrackStreamInfo>($"TrackStreamInfo: TrackId [{trackId}] Unable to Find Track [{trackFileInfo.FullName}]");
                }
            }
            var contentDurationTimeSpan = TimeSpan.FromMilliseconds((double)(track.Duration ?? 0));
            var info = new TrackStreamInfo
            {
                FileName = this.HttpEncoder.UrlEncode(track.FileName).ToContentDispositionFriendly(),
                ContentDisposition = $"attachment; filename=\"{ this.HttpEncoder.UrlEncode(track.FileName).ToContentDispositionFriendly() }\"",
                ContentDuration = contentDurationTimeSpan.TotalSeconds.ToString(),
            };
            var cacheTimeout = 86400; // 24 hours
            var contentLength = (endBytes - beginBytes) + 1;
            info.Track = new DataToken
            {
                Text = track.Title,
                Value = track.RoadieId.ToString()
            };
            info.BeginBytes = beginBytes;
            info.EndBytes = endBytes;
            info.ContentRange = $"bytes {beginBytes}-{endBytes}/{contentLength}";
            info.ContentLength = contentLength.ToString();
            info.IsFullRequest = beginBytes == 0 && endBytes == (trackFileInfo.Length - 1);
            info.IsEndRangeRequest = beginBytes > 0 && endBytes != (trackFileInfo.Length - 1);
            info.LastModified = (track.LastUpdated ?? track.CreatedDate).ToString("R");
            info.Etag = track.Etag;
            info.CacheControl = $"public, max-age={ cacheTimeout.ToString() } ";
            info.Expires = DateTime.UtcNow.AddMinutes(cacheTimeout).ToString("R");
            int bytesToRead = (int)(endBytes - beginBytes) + 1;
            byte[] trackBytes = new byte[bytesToRead];
            using (var fs = trackFileInfo.OpenRead())
            {
                try
                {
                    fs.Seek(beginBytes, SeekOrigin.Begin);
                    var r = fs.Read(trackBytes, 0, bytesToRead);
                }
                catch (Exception ex)
                {
                    return new OperationResult<TrackStreamInfo>(ex);
                }
            }
            info.Bytes = trackBytes;
            return new OperationResult<TrackStreamInfo>
            {
                IsSuccess = true,
                Data = info
            };
        }

        private Task<OperationResult<Track>> TrackByIdAction(Guid id, IEnumerable<string> includes)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();

            var track = this.GetTrack(id);

            if (track == null)
            {
                return Task.FromResult(new OperationResult<Track>(true, string.Format("Track Not Found [{0}]", id)));
            }
            var result = track.Adapt<Track>();
            result.IsLocked = (track.IsLocked ?? false) ||
                              (track.ReleaseMedia.IsLocked ?? false) ||
                              (track.ReleaseMedia.Release.IsLocked ?? false) ||
                              (track.ReleaseMedia.Release.Artist.IsLocked ?? false);
            result.Thumbnail = base.MakeTrackThumbnailImage(id);
            result.MediumThumbnail = base.MakeThumbnailImage(id, "track", this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height);
            result.ReleaseMediaId = track.ReleaseMedia.RoadieId.ToString();
            result.Artist = ArtistList.FromDataArtist(track.ReleaseMedia.Release.Artist, this.MakeArtistThumbnailImage(track.ReleaseMedia.Release.Artist.RoadieId));
            result.ArtistThumbnail = this.MakeArtistThumbnailImage(track.ReleaseMedia.Release.Artist.RoadieId);
            result.Release = ReleaseList.FromDataRelease(track.ReleaseMedia.Release, track.ReleaseMedia.Release.Artist, this.HttpContext.BaseUrl, this.MakeArtistThumbnailImage(track.ReleaseMedia.Release.Artist.RoadieId), this.MakeReleaseThumbnailImage(track.ReleaseMedia.Release.RoadieId));
            result.ReleaseThumbnail = this.MakeReleaseThumbnailImage(track.ReleaseMedia.Release.RoadieId);
            if (track.ArtistId.HasValue)
            {
                var trackArtist = this.DbContext.Artists.FirstOrDefault(x => x.Id == track.ArtistId);
                if (trackArtist == null)
                {
                    this.Logger.LogWarning($"Unable to find Track Artist [{ track.ArtistId }");
                }
                else
                {
                    result.TrackArtist = ArtistList.FromDataArtist(trackArtist, this.MakeArtistThumbnailImage(trackArtist.RoadieId));
                    result.TrackArtistToken = result.TrackArtist.Artist;
                    result.TrackArtistThumbnail = this.MakeArtistThumbnailImage(trackArtist.RoadieId);
                }
            }
            if (includes != null && includes.Any())
            {
                if (includes.Contains("stats"))
                {
                    result.Statistics = new Library.Models.Statistics.TrackStatistics
                    {
                        FileSizeFormatted = ((long?)track.FileSize).ToFileSize(),
                        Time = new TimeInfo((decimal)track.Duration).ToFullFormattedString(),
                        PlayedCount = track.PlayedCount
                    };
                    var userTracks = (from t in this.DbContext.Tracks
                                      join ut in this.DbContext.UserTracks on t.Id equals ut.TrackId
                                      where t.Id == track.Id
                                      select ut).ToArray();
                    if (userTracks != null && userTracks.Any())
                    {
                        result.Statistics.DislikedCount = userTracks.Count(x => x.IsDisliked ?? false);
                        result.Statistics.FavoriteCount = userTracks.Count(x => x.IsFavorite ?? false);
                    }
                }
            }

            sw.Stop();
            return Task.FromResult(new OperationResult<Track>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            });
        }
    }
}