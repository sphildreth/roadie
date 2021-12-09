using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Imaging;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Releases;
using Roadie.Library.Models.Statistics;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public class TrackService : ServiceBase, ITrackService
    {
        private IAdminService AdminService { get; }

        private IAudioMetaDataHelper AudioMetaDataHelper { get; }

        private IBookmarkService BookmarkService { get; }

        public TrackService(
            IRoadieSettings configuration,
            IRoadieDbContext dbContext,
            ICacheManager cacheManager,
            ILogger logger)
            : base(configuration, null, dbContext, cacheManager, logger, null)
        {
        }

        public TrackService(
            IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            IRoadieDbContext dbContext,
            ICacheManager cacheManager,
            ILogger<TrackService> logger,
            IBookmarkService bookmarkService,
            IAdminService adminService,
            IAudioMetaDataHelper audioMetaDataHelper)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
            BookmarkService = bookmarkService;
            AudioMetaDataHelper = audioMetaDataHelper;
            AdminService = adminService;
        }

        private async Task<OperationResult<Track>> TrackByIdActionAsync(Guid id, IEnumerable<string> includes)
        {
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            var sw = Stopwatch.StartNew();
            sw.Start();

            tsw.Restart();
            var track = await GetTrack(id).ConfigureAwait(false);
            tsw.Stop();
            timings.Add("getTrack", tsw.ElapsedMilliseconds);

            if (track == null)
            {
                return new OperationResult<Track>(true, $"Track Not Found [{id}]");
            }
            tsw.Restart();
            var result = track.Adapt<Track>();
            result.IsLocked = (track.IsLocked ?? false) ||
                              (track.ReleaseMedia.IsLocked ?? false) ||
                              (track.ReleaseMedia.Release.IsLocked ?? false) ||
                              (track.ReleaseMedia.Release.Artist.IsLocked ?? false);
            result.Thumbnail = ImageHelper.MakeTrackThumbnailImage(Configuration, HttpContext, id);
            result.MediumThumbnail = ImageHelper.MakeThumbnailImage(Configuration, HttpContext, id, "track", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height);
            result.ReleaseMediaId = track.ReleaseMedia.RoadieId.ToString();
            result.Artist = ArtistList.FromDataArtist(track.ReleaseMedia.Release.Artist,
                ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, track.ReleaseMedia.Release.Artist.RoadieId));
            result.ArtistThumbnail = ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, track.ReleaseMedia.Release.Artist.RoadieId);
            result.Release = ReleaseList.FromDataRelease(track.ReleaseMedia.Release, track.ReleaseMedia.Release.Artist,
                HttpContext.BaseUrl, ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, track.ReleaseMedia.Release.Artist.RoadieId),
                ImageHelper.MakeReleaseThumbnailImage(Configuration, HttpContext, track.ReleaseMedia.Release.RoadieId));
            result.ReleaseThumbnail = ImageHelper.MakeReleaseThumbnailImage(Configuration, HttpContext, track.ReleaseMedia.Release.RoadieId);
            tsw.Stop();
            timings.Add("adapt", tsw.ElapsedMilliseconds);
            if (track.ArtistId.HasValue)
            {
                tsw.Restart();
                var trackArtist = DbContext.Artists.FirstOrDefault(x => x.Id == track.ArtistId);
                if (trackArtist == null)
                {
                    Logger.LogWarning($"Unable to find Track Artist [{track.ArtistId}");
                }
                else
                {
                    result.TrackArtist =
                        ArtistList.FromDataArtist(trackArtist, ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, trackArtist.RoadieId));
                    result.TrackArtistToken = result.TrackArtist.Artist;
                    result.TrackArtistThumbnail = ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, trackArtist.RoadieId);
                }
                tsw.Stop();
                timings.Add("trackArtist", tsw.ElapsedMilliseconds);
            }

            if (includes?.Any() == true)
            {
                if (includes.Contains("credits"))
                {
                    tsw.Restart();

                    result.Credits = (await (from c in DbContext.Credits
                                             join cc in DbContext.CreditCategory on c.CreditCategoryId equals cc.Id
                                             join a in DbContext.Artists on c.ArtistId equals a.Id into agg
                                             from a in agg.DefaultIfEmpty()
                                             where c.TrackId == track.Id
                                             select new { c, cc, a })
                                             .ToListAsync().ConfigureAwait(false))
                                             .Select(x => new CreditList
                                             {
                                                 Id = x.c.RoadieId,
                                                 Artist = x.a == null ? null : ArtistList.FromDataArtist(x.a, ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, x.a.RoadieId)),
                                                 Category = new DataToken
                                                 {
                                                     Text = x.cc.Name,
                                                     Value = x.cc.RoadieId.ToString()
                                                 },
                                                 CreditName = x.a?.Name ?? x.c.CreditToName,
                                                 Description = x.c.Description
                                             }).ToArray();
                    tsw.Stop();
                    timings.Add("credits", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("stats"))
                {
                    tsw.Restart();
                    result.Statistics = new TrackStatistics
                    {
                        FileSizeFormatted = ((long?)track.FileSize).ToFileSize(),
                        Time = new TimeInfo((decimal)track.Duration).ToFullFormattedString(),
                        PlayedCount = track.PlayedCount
                    };
                    var userTracks = (from t in DbContext.Tracks
                                      join ut in DbContext.UserTracks on t.Id equals ut.TrackId
                                      where t.Id == track.Id
                                      select ut).ToArray();
                    if (userTracks?.Any() == true)
                    {
                        result.Statistics.DislikedCount = userTracks.Count(x => x.IsDisliked ?? false);
                        result.Statistics.FavoriteCount = userTracks.Count(x => x.IsFavorite ?? false);
                    }
                    tsw.Stop();
                    timings.Add("stats", tsw.ElapsedMilliseconds);
                }

                if (includes.Contains("comments"))
                {
                    tsw.Restart();
                    var trackComments = DbContext.Comments.Include(x => x.User).Where(x => x.TrackId == track.Id)
                        .OrderByDescending(x => x.CreatedDate).ToArray();
                    if (trackComments.Length > 0)
                    {
                        var comments = new List<Comment>();
                        var commentIds = trackComments.Select(x => x.Id).ToArray();
                        var userCommentReactions = (from cr in DbContext.CommentReactions
                                                    where commentIds.Contains(cr.CommentId)
                                                    select cr).ToArray();
                        foreach (var trackComment in trackComments)
                        {
                            var comment = trackComment.Adapt<Comment>();
                            comment.DatabaseId = trackComment.Id;
                            comment.User = UserList.FromDataUser(trackComment.User,
                                ImageHelper.MakeUserThumbnailImage(Configuration, HttpContext, trackComment.User.RoadieId));
                            comment.DislikedCount = userCommentReactions.Count(x =>
                                x.CommentId == trackComment.Id && x.ReactionValue == CommentReaction.Dislike);
                            comment.LikedCount = userCommentReactions.Count(x =>
                                x.CommentId == trackComment.Id && x.ReactionValue == CommentReaction.Like);
                            comments.Add(comment);
                        }

                        result.Comments = comments;
                    }
                    tsw.Stop();
                    timings.Add("comments", tsw.ElapsedMilliseconds);
                }
            }

            sw.Stop();
            Logger.LogInformation($"ByIdAction: Track `{ track }`: includes [{includes.ToCSV()}], timings: [{ timings.ToTimings() }]");
            return new OperationResult<Track>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<Track>> ByIdAsyncAsync(User roadieUser, Guid id, IEnumerable<string> includes)
        {
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = $"urn:track_by_id_operation:{id}:{(includes == null ? "0" : string.Join("|", includes))}";
            var result = await CacheManager.GetAsync(cacheKey, async () =>
            {
                tsw.Restart();
                var rr = await TrackByIdActionAsync(id, includes).ConfigureAwait(false);
                tsw.Stop();
                timings.Add(nameof(TrackByIdActionAsync), tsw.ElapsedMilliseconds);
                return rr;
            }, data.Track.CacheRegionUrn(id)).ConfigureAwait(false);
            if (result?.Data != null && roadieUser != null)
            {
                tsw.Restart();
                var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
                tsw.Stop();
                timings.Add("getUser", tsw.ElapsedMilliseconds);

                tsw.Restart();
                var track = await GetTrack(id).ConfigureAwait(false);
                tsw.Stop();
                timings.Add("getTrack", tsw.ElapsedMilliseconds);

                result.Data.TrackPlayUrl = MakeTrackPlayUrl(user, HttpContext.BaseUrl, track.RoadieId);

                tsw.Restart();
                var userBookmarkResult = await BookmarkService.ListAsync(roadieUser, new PagedRequest(), false, BookmarkType.Track).ConfigureAwait(false);
                if (userBookmarkResult.IsSuccess)
                {
                    result.Data.UserBookmarked = userBookmarkResult?.Rows?.FirstOrDefault(x => x?.Bookmark?.Value == track?.RoadieId.ToString()) != null;
                }
                tsw.Stop();
                timings.Add("userBookmarks", tsw.ElapsedMilliseconds);

                tsw.Restart();
                var userTrack = DbContext.UserTracks.FirstOrDefault(x => x.TrackId == track.Id && x.UserId == roadieUser.Id);
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
                tsw.Stop();
                timings.Add("userTracks", tsw.ElapsedMilliseconds);

                if (result.Data.Comments.Any())
                {
                    tsw.Restart();
                    var commentIds = result.Data.Comments.Select(x => x.DatabaseId).ToArray();
                    var userCommentReactions = (from cr in DbContext.CommentReactions
                                                where commentIds.Contains(cr.CommentId)
                                                where cr.UserId == roadieUser.Id
                                                select cr).ToArray();
                    foreach (var comment in result.Data.Comments)
                    {
                        var userCommentReaction = Array.Find(userCommentReactions, x => x.CommentId == comment.DatabaseId);
                        comment.IsDisliked = userCommentReaction?.ReactionValue == CommentReaction.Dislike;
                        comment.IsLiked = userCommentReaction?.ReactionValue == CommentReaction.Like;
                    }
                    tsw.Stop();
                    timings.Add("userComments", tsw.ElapsedMilliseconds);
                }
            }

            sw.Stop();
            Logger.LogInformation($"ById Track: `{ result?.Data }`, includes [{ includes.ToCSV() }], timings [{ timings.ToTimings() }]");
            return new OperationResult<Track>(result.Messages)
            {
                Data = result?.Data,
                Errors = result?.Errors,
                IsNotFoundResult = result?.IsNotFoundResult ?? false,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public static long DetermineByteEndFromHeaders(IHeaderDictionary headers, long fileLength)
        {
            var defaultFileLength = fileLength - 1;
            if (headers?.Any(x => x.Key == "Range") != true)
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
                rangeBegin = rangeBegin.Replace("bytes=", string.Empty);
                var parts = rangeBegin.Split('-');
                rangeBegin = parts[0];
                if (parts.Length > 1)
                {
                    rangeEnd = parts[1];
                }

                if (!string.IsNullOrEmpty(rangeEnd))
                {
                    result = long.TryParse(rangeEnd, out var outValue) ? (int?)outValue : null;
                }
            }

            return result ?? defaultFileLength;
        }

        public static long DetermineByteStartFromHeaders(IHeaderDictionary headers)
        {
            if (headers?.Any(x => x.Key == "Range") != true)
            {
                return 0;
            }

            long result = 0;
            var rangeHeader = headers["Range"];
            var rangeBegin = rangeHeader.FirstOrDefault();
            if (!string.IsNullOrEmpty(rangeBegin))
            {
                //bytes=0-
                rangeBegin = rangeBegin.Replace("bytes=", string.Empty);
                var parts = rangeBegin.Split('-');
                rangeBegin = parts[0];
                if (!string.IsNullOrEmpty(rangeBegin))
                {
                    long.TryParse(rangeBegin, out result);
                }
            }

            return result;
        }

        public async Task<Library.Models.Pagination.PagedResult<TrackList>> ListAsync(PagedRequest request, User roadieUser, bool? doRandomize = false, Guid? releaseId = null)
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

                var favoriteTrackIds = new int[0].AsQueryable();
                if (request.FilterFavoriteOnly)
                {
                    favoriteTrackIds = from t in DbContext.Tracks
                                       join ut in DbContext.UserTracks on t.Id equals ut.TrackId
                                       where ut.UserId == roadieUser.Id
                                       where ut.IsFavorite ?? false
                                       select t.Id;
                }

                var playListTrackPositions = new Dictionary<int, int>();
                var playlistTrackIds = new int[0];
                if (request.FilterToPlaylistId.HasValue)
                {
                    var playlistTrackInfos = await (from plt in DbContext.PlaylistTracks
                                                    join p in DbContext.Playlists on plt.PlayListId equals p.Id
                                                    join t in DbContext.Tracks on plt.TrackId equals t.Id
                                                    where p.RoadieId == request.FilterToPlaylistId.Value
                                                    orderby plt.ListNumber
                                                    select new KeyValuePair<int, int>(t.Id, plt.ListNumber)).ToArrayAsync().ConfigureAwait(false);

                    if(!request.FilterFavoriteOnly && 
                        request.FilterToPlaylistId == PlaylistService.DynamicFavoritePlaylistId)
                    {
                        var dynamicPlaylistFavoriteTrackIds = await (from ut in DbContext.UserTracks
                                                                     join t in DbContext.Tracks on ut.TrackId equals t.Id
                                                                     where ut.UserId == roadieUser.Id
                                                                     where ut.IsFavorite == true
                                                                     orderby t.CreatedDate descending
                                                                     select t.Id).ToArrayAsync().ConfigureAwait(false);
                        playlistTrackInfos = dynamicPlaylistFavoriteTrackIds.Select((x,i) => new KeyValuePair<int, int>(x, i+1)).ToArray();
                    }

                    rowCount = playlistTrackInfos.Length;
                    playListTrackPositions = playlistTrackInfos
                                              .Skip(request.SkipValue)
                                              .Take(request.LimitValue)
                                              .ToDictionary(x => x.Key, x => x.Value);
                    playlistTrackIds = playListTrackPositions.Select(x => x.Key).ToArray();
                    request.Sort = "TrackNumber";
                    request.Order = "ASC";
                    request.Page = 1; // Set back to first or it skips already paged tracks for playlist
                    request.SkipValue = 0;
                }

                var collectionTrackIds = new int[0];
                if (request.FilterToCollectionId.HasValue)
                {
                    request.Limit = roadieUser?.PlayerTrackLimit ?? 50;

                    collectionTrackIds = await (from cr in DbContext.CollectionReleases
                                                join c in DbContext.Collections on cr.CollectionId equals c.Id
                                                join r in DbContext.Releases on cr.ReleaseId equals r.Id
                                                join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                                join t in DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                                where c.RoadieId == request.FilterToCollectionId.Value
                                                orderby cr.ListNumber, rm.MediaNumber, t.TrackNumber
                                                select t.Id)
                                          .Skip(request.SkipValue)
                                          .Take(request.LimitValue)
                                          .ToArrayAsync().ConfigureAwait(false);
                }

                IQueryable<int> topTrackids = null;
                if (request.FilterTopPlayedOnly)
                {
                    // Get request number of top played songs for artist
                    topTrackids = (from t in DbContext.Tracks
                                   join ut in DbContext.UserTracks on t.Id equals ut.TrackId
                                   join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                   join r in DbContext.Releases on rm.ReleaseId equals r.Id
                                   join a in DbContext.Artists on r.ArtistId equals a.Id
                                   where a.RoadieId == request.FilterToArtistId
                                   orderby ut.PlayedCount descending
                                   select t.Id
                                   ).Skip(request.SkipValue)
                                    .Take(request.LimitValue);
                }

                int[] randomTrackIds = null;
                SortedDictionary<int, int> randomTrackData = null;
                if (doRandomize ?? false)
                {
                    var randomLimit = roadieUser?.RandomReleaseLimit ?? request.LimitValue;
                    randomTrackData = await DbContext.RandomTrackIdsAsync(roadieUser?.Id ?? -1, randomLimit, request.FilterFavoriteOnly, request.FilterRatedOnly).ConfigureAwait(false);
                    randomTrackIds = randomTrackData.Select(x => x.Value).ToArray();
                    rowCount = DbContext.Releases.Count();
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
#pragma warning disable IDE0057 // Use range operator
                        request.Filter = filter.Substring(1, filter.Length - 2);
#pragma warning restore IDE0057 // Use range operator
                    }
                }

                // Did this for performance against the Track table, with just * selects the table scans are too much of a performance hit.
                var resultQuery = from t in DbContext.Tracks
                                  join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                  join r in DbContext.Releases on rm.ReleaseId equals r.Id
                                  join releaseArtist in DbContext.Artists on r.ArtistId equals releaseArtist.Id
                                  join trackArtist in DbContext.Artists on t.ArtistId equals trackArtist.Id into tas
                                  from trackArtist in tas.DefaultIfEmpty()
                                  where t.Hash != null
                                  where randomTrackIds == null || randomTrackIds.Contains(t.Id)
                                  where filterToTrackIds == null || filterToTrackIds.Contains(t.RoadieId)
                                  where releaseId == null || r.RoadieId == releaseId
                                  where request.FilterMinimumRating == null || t.Rating >= request.FilterMinimumRating.Value
                                  where string.IsNullOrEmpty(normalizedFilterValue) ||
                                        (trackArtist != null && trackArtist.Name.ToLower().Contains(normalizedFilterValue)) ||
                                        t.Title.ToLower().Contains(normalizedFilterValue) ||
                                        t.AlternateNames.Contains(normalizedFilterValue) ||
                                        t.PartTitles.ToLower().Contains(normalizedFilterValue)
                                  where !isEqualFilter ||
                                         t.Title.ToLower().Equals(normalizedFilterValue) ||
                                         t.AlternateNames.ToLower().Equals(normalizedFilterValue) ||
                                         t.PartTitles.ToLower().Equals(request.FilterValue)
                                  where !request.FilterFavoriteOnly || favoriteTrackIds.Contains(t.Id)
                                  where request.FilterToPlaylistId == null || playlistTrackIds.Contains(t.Id)
                                  where !request.FilterTopPlayedOnly || topTrackids.Contains(t.Id)
                                  where request.FilterToArtistId == null || (t.TrackArtist != null && t.TrackArtist.RoadieId == request.FilterToArtistId) || r.Artist.RoadieId == request.FilterToArtistId
                                  where !request.IsHistoryRequest || t.PlayedCount > 0
                                  where request.FilterToCollectionId == null || collectionTrackIds.Contains(t.Id)
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
                                          t.Status,
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
                                          ArtistThumbnail = ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, releaseArtist.RoadieId),
                                          CreatedDate = r.CreatedDate,
                                          Duration = r.Duration,
                                          LastPlayed = r.LastPlayed,
                                          LastUpdated = r.LastUpdated,
                                          LibraryStatus = r.LibraryStatus,
                                          MediaCount = r.MediaCount,
                                          Rating = r.Rating,
                                          Rank = (double?)r.Rank,
                                          ReleaseDateDateTime = r.ReleaseDate,
                                          ReleasePlayUrl = $"{HttpContext.BaseUrl}/play/release/{r.RoadieId}",
                                          Status = r.Status,
                                          Thumbnail = ImageHelper.MakeReleaseThumbnailImage(Configuration, HttpContext, r.RoadieId),
                                          TrackCount = r.TrackCount,
                                          TrackPlayedCount = r.PlayedCount
                                      },
                                      ta = trackArtist == null
                                          ? null
                                          : new ArtistList
                                          {
                                              DatabaseId = trackArtist.Id,
                                              Id = trackArtist.RoadieId,
                                              Artist = new DataToken
                                              { Text = trackArtist.Name, Value = trackArtist.RoadieId.ToString() },
                                              Rating = trackArtist.Rating,
                                              Rank = (double?)trackArtist.Rank,
                                              CreatedDate = trackArtist.CreatedDate,
                                              LastUpdated = trackArtist.LastUpdated,
                                              LastPlayed = trackArtist.LastPlayed,
                                              PlayedCount = trackArtist.PlayedCount,
                                              ReleaseCount = trackArtist.ReleaseCount,
                                              TrackCount = trackArtist.TrackCount,
                                              SortName = trackArtist.SortName,
                                              Thumbnail = ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, trackArtist.RoadieId)
                                          },
                                      ra = new ArtistList
                                      {
                                          DatabaseId = releaseArtist.Id,
                                          Id = releaseArtist.RoadieId,
                                          Artist = new DataToken
                                          { Text = releaseArtist.Name, Value = releaseArtist.RoadieId.ToString() },
                                          Rating = releaseArtist.Rating,
                                          Rank = (double?)releaseArtist.Rank,
                                          CreatedDate = releaseArtist.CreatedDate,
                                          LastUpdated = releaseArtist.LastUpdated,
                                          LastPlayed = releaseArtist.LastPlayed,
                                          PlayedCount = releaseArtist.PlayedCount,
                                          ReleaseCount = releaseArtist.ReleaseCount,
                                          TrackCount = releaseArtist.TrackCount,
                                          SortName = releaseArtist.SortName,
                                          Thumbnail = ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, releaseArtist.RoadieId)
                                      }
                                  };

                if (!string.IsNullOrEmpty(request.FilterValue) && request.FilterValue.StartsWith("#"))
                {
                    // Find any releases by tags
                    var tagValue = request.FilterValue.Replace("#", string.Empty);
                    resultQuery = resultQuery.Where(x => x.ti.Tags != null && x.ti.Tags.Contains(tagValue));
                }

                var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
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
                        Status = x.ti.Status,
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
                        Thumbnail = ImageHelper.MakeTrackThumbnailImage(Configuration, HttpContext, x.ti.RoadieId),
                        Title = x.ti.Title,
                        TrackArtist = x.ta,
                        TrackNumber = x.ti.TrackNumber,
                        TrackPlayUrl = MakeTrackPlayUrl(user, HttpContext.BaseUrl, x.ti.RoadieId)
                    });
                string sortBy = null;

                rowCount ??= result.Count();
                TrackList[] rows = null;

                if (!doRandomize ?? false)
                {
                    if (request.Action == User.ActionKeyUserRated)
                    {
                        sortBy = string.IsNullOrEmpty(request.Sort)
                            ? request.OrderValue(new Dictionary<string, string> { { "UserTrack.Rating", "DESC" }, { "MediaNumber", "ASC" }, { "TrackNumber", "ASC" } })
                            : request.OrderValue();
                    }
                    else if (request.Sort == "Rating")
                    {
                        // The request is to sort tracks by Rating if the artist only has a few tracks rated then order by those then order by played (put most popular after top rated)
                        sortBy = request.OrderValue(new Dictionary<string, string> { { "Rating", request.Order }, { "PlayedCount", request.Order } });
                    }
                    else
                    {
                        sortBy = string.IsNullOrEmpty(request.Sort)
                            ? request.OrderValue(new Dictionary<string, string> { { "Release.Release.Text", "ASC" }, { "MediaNumber", "ASC" }, { "TrackNumber", "ASC" } })
                            : request.OrderValue();
                    }
                }

                if (doRandomize ?? false)
                {
                    var resultData = await result.ToArrayAsync().ConfigureAwait(false);
                    rows = (from r in resultData
                            join ra in randomTrackData on r.DatabaseId equals ra.Value
                            orderby ra.Key
                            select r
                           ).ToArray();
                }
                else
                {
                    rows = await result
                                .OrderBy(sortBy)
                                .Skip(request.SkipValue)
                                .Take(request.LimitValue)
                                .ToArrayAsync().ConfigureAwait(false);
                }
                if (rows.Length > 0 && roadieUser != null)
                {
                    var rowIds = rows.Select(x => x.DatabaseId).ToArray();
                    var userTrackRatings = await (from ut in DbContext.UserTracks
                                                  where ut.UserId == roadieUser.Id
                                                  where rowIds.Contains(ut.TrackId)
                                                  select ut).ToArrayAsync().ConfigureAwait(false);
                    foreach (var userTrackRating in userTrackRatings)
                    {
                        var row = Array.Find(rows, x => x.DatabaseId == userTrackRating.TrackId);
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
                    var userReleaseRatings = await (from ur in DbContext.UserReleases
                                                    where ur.UserId == roadieUser.Id
                                                    where releaseIds.Contains(ur.ReleaseId)
                                                    select ur).ToArrayAsync().ConfigureAwait(false);
                    foreach (var userReleaseRating in userReleaseRatings)
                    {
                        foreach (var row in rows.Where(x => x.Release.DatabaseId == userReleaseRating.ReleaseId))
                        {
                            row.Release.UserRating = userReleaseRating.Adapt<UserRelease>();
                        }
                    }

                    var artistIds = rows.Select(x => x.Artist.DatabaseId).ToArray();
                    if (artistIds?.Any() == true)
                    {
                        var userArtistRatings = await (from ua in DbContext.UserArtists
                                                       where ua.UserId == roadieUser.Id
                                                       where artistIds.Contains(ua.ArtistId)
                                                       select ua).ToArrayAsync().ConfigureAwait(false);
                        foreach (var userArtistRating in userArtistRatings)
                        {
                            foreach (var artistTrack in rows.Where(
                                x => x.Artist.DatabaseId == userArtistRating.ArtistId))
                            {
                                artistTrack.Artist.UserRating = userArtistRating.Adapt<UserArtist>();
                            }
                        }
                    }

                    var trackArtistIds = rows.Where(x => x.TrackArtist != null).Select(x => x.TrackArtist.DatabaseId).ToArray();
                    if (trackArtistIds?.Any() == true)
                    {
                        var userTrackArtistRatings = await (from ua in DbContext.UserArtists
                                                            where ua.UserId == roadieUser.Id
                                                            where trackArtistIds.Contains(ua.ArtistId)
                                                            select ua).ToArrayAsync().ConfigureAwait(false);
                        if (userTrackArtistRatings?.Any() == true)
                        {
                            foreach (var userTrackArtistRating in userTrackArtistRatings)
                            {
                                foreach (var artistTrack in rows.Where(x =>
                                    x.TrackArtist != null &&
                                    x.TrackArtist.DatabaseId == userTrackArtistRating.ArtistId))
                                {
                                    artistTrack.Artist.UserRating = userTrackArtistRating.Adapt<UserArtist>();
                                }
                            }
                        }
                    }
                }

                if (rows.Length > 0)
                {
                    var rowIds = rows.Select(x => x.DatabaseId).ToArray();
                    var favoriteUserTrackRatings = await (from ut in DbContext.UserTracks
                                                          where ut.IsFavorite ?? false
                                                          where rowIds.Contains(ut.TrackId)
                                                          select ut).ToArrayAsync().ConfigureAwait(false);
                    foreach (var row in rows)
                    {
                        row.FavoriteCount = favoriteUserTrackRatings.Count(x => x.TrackId == row.DatabaseId);
                        row.TrackNumber = playListTrackPositions.ContainsKey(row.DatabaseId) ? playListTrackPositions[row.DatabaseId] : row.TrackNumber;
                    }
                }

                if (playListTrackPositions.Count > 0)
                {
                    rows = rows.OrderBy(x => x.TrackNumber).ToArray();
                }

                sw.Stop();
                return new Library.Models.Pagination.PagedResult<TrackList>
                {
                    TotalCount = rowCount ?? 0,
                    CurrentPage = request.PageValue,
                    TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                    OperationTime = sw.ElapsedMilliseconds,
                    Rows = rows
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error In List, Request [{0}], User [{1}]", CacheManager.CacheSerializer.Serialize(request), roadieUser);
                return new Library.Models.Pagination.PagedResult<TrackList>
                {
                    Message = "An Error has occured"
                };
            }
        }

        /// <summary>
        ///     Fast as possible check if exists and return minimum information on Track
        /// </summary>
        public OperationResult<Track> StreamCheckAndInfo(User roadieUser, Guid id)
        {
            var track = DbContext.Tracks.FirstOrDefault(x => x.RoadieId == id);
            if (track == null)
            {
                return new OperationResult<Track>(true, $"Track Not Found [{id}]");
            }

            return new OperationResult<Track>
            {
                Data = track.Adapt<Track>(),
                IsSuccess = true
            };
        }

        public async Task<OperationResult<TrackStreamInfo>> TrackStreamInfoAsync(Guid trackId, long beginBytes, long endBytes, User roadieUser)
        {
            var track = DbContext.Tracks.FirstOrDefault(x => x.RoadieId == trackId);
            if (!(track?.IsValid ?? true))
            {
                // Not Found try recanning release
                var release = (from r in DbContext.Releases
                               join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                               where rm.Id == track.ReleaseMediaId
                               select r).FirstOrDefault();
                if (!release.IsLocked ?? false && roadieUser != null)
                {
                    await AdminService.ScanReleaseAsync(new Library.Identity.User
                    {
                        Id = roadieUser.Id.Value
                    }, release.RoadieId, false, true).ConfigureAwait(false);
                    track = DbContext.Tracks.FirstOrDefault(x => x.RoadieId == trackId);
                }
                else
                {
                    Logger.LogWarning($"TrackStreamInfo: Track [{ trackId }] was invalid but release [{ release.RoadieId }] is locked, did not rescan.");
                }
                if (track == null)
                {
                    return new OperationResult<TrackStreamInfo>($"TrackStreamInfo: Unable To Find Track [{trackId}]");
                }
                if (!track.IsValid)
                {
                    return new OperationResult<TrackStreamInfo>($"TrackStreamInfo: Invalid Track. Track Id [{trackId}], FilePath [{track.FilePath}], Filename [{track.FileName}]");
                }
            }
            string trackPath = null;
            try
            {
                trackPath = track.PathToTrack(Configuration);
            }
            catch (Exception ex)
            {
                return new OperationResult<TrackStreamInfo>(ex);
            }

            var trackFileInfo = new FileInfo(trackPath);
            if (!trackFileInfo.Exists)
            {
                // Not Found try recanning release
                var release = (from r in DbContext.Releases
                               join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                               where rm.Id == track.ReleaseMediaId
                               select r).FirstOrDefault();
                if (!release.IsLocked ?? false && roadieUser != null)
                {
                    await AdminService.ScanReleaseAsync(new Library.Identity.User
                    {
                        Id = roadieUser.Id.Value
                    }, release.RoadieId, false, true).ConfigureAwait(false);
                }

                track = DbContext.Tracks.FirstOrDefault(x => x.RoadieId == trackId);
                if (track == null)
                {
                    return new OperationResult<TrackStreamInfo>($"TrackStreamInfo: Unable To Find Track [{trackId}]");
                }

                try
                {
                    trackPath = track.PathToTrack(Configuration);
                }
                catch (Exception ex)
                {
                    return new OperationResult<TrackStreamInfo>(ex);
                }

                if (!trackFileInfo.Exists)
                {
                    track.UpdateTrackMissingFile();
                    await DbContext.SaveChangesAsync().ConfigureAwait(false);
                    return new OperationResult<TrackStreamInfo>(
                        $"TrackStreamInfo: TrackId [{trackId}] Unable to Find Track [{trackFileInfo.FullName}]");
                }
            }

            var contentDurationTimeSpan = TimeSpan.FromMilliseconds(track.Duration ?? 0);
            var info = new TrackStreamInfo
            {
                FileName = HttpEncoder?.UrlEncode(track.FileName).ToContentDispositionFriendly(),
                ContentDisposition = $"attachment; filename=\"{HttpEncoder?.UrlEncode(track.FileName).ToContentDispositionFriendly()}\"",
                ContentDuration = contentDurationTimeSpan.TotalSeconds.ToString()
            };
            var contentLength = endBytes - beginBytes + 1;
            info.Track = new DataToken
            {
                Text = track.Title,
                Value = track.RoadieId.ToString()
            };
            info.BeginBytes = beginBytes;
            info.EndBytes = endBytes;
            info.ContentRange = $"bytes {beginBytes}-{endBytes}/{contentLength}";
            info.ContentLength = contentLength.ToString();
            info.IsFullRequest = beginBytes == 0 && endBytes == trackFileInfo.Length - 1;
            info.IsEndRangeRequest = beginBytes > 0 && endBytes != trackFileInfo.Length - 1;
            info.LastModified = (track.LastUpdated ?? track.CreatedDate).ToString("R");

            info.CacheControl = "no-store, must-revalidate, no-cache, max-age=0";
            info.Pragma = "no-cache";
            info.Expires = "Mon, 01 Jan 1990 00:00:00 GMT";

            var bytesToRead = (int)(endBytes - beginBytes) + 1;
            var trackBytes = new byte[bytesToRead];
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

        public async Task<OperationResult<bool>> UpdateTrackAsync(User user, Track model)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var track = DbContext.Tracks
                .Include(x => x.ReleaseMedia)
                .Include(x => x.ReleaseMedia.Release)
                .Include(x => x.ReleaseMedia.Release.Artist)
                .FirstOrDefault(x => x.RoadieId == model.Id);
            if (track == null)
            {
                return new OperationResult<bool>(true, $"Track Not Found [{model.Id}]");
            }

            try
            {
                var originalTitle = track.Title;
                var originalTrackNumber = track.TrackNumber;
                var originalFilename = track.PathToTrack(Configuration);
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
                track.PartTitles = model.PartTitlesList?.Any() != true
                    ? null
                    : string.Join("\n", model.PartTitlesList);

                data.Artist trackArtist = null;
                if (model.TrackArtistToken != null)
                {
                    var artistId = SafeParser.ToGuid(model.TrackArtistToken.Value);
                    if (artistId.HasValue)
                    {
                        trackArtist = await GetArtist(artistId.Value).ConfigureAwait(false);
                        if (trackArtist != null)
                        {
                            track.ArtistId = trackArtist.Id;
                        }
                    }
                }
                else
                {
                    track.ArtistId = null;
                }

                if (model.Credits?.Any() != true)
                {
                    // Delete all existing credits for track
                    var trackCreditsToDelete = (from c in DbContext.Credits
                                                where c.TrackId == track.Id
                                                select c).ToArray();
                    DbContext.Credits.RemoveRange(trackCreditsToDelete);
                }
                else if (model.Credits?.Any() == true)
                {
                    var trackCreditIds = model.Credits.Select(x => x.Id).ToArray();
                    // Delete any credits not given in model (removed by edit operation)
                    var trackCreditsToDelete = (from c in DbContext.Credits
                                                where c.TrackId == track.Id
                                                where !trackCreditIds.Contains(c.RoadieId)
                                                select c).ToArray();
                    DbContext.Credits.RemoveRange(trackCreditsToDelete);
                    // Update any existing
                    foreach (var credit in model.Credits)
                    {
                        var trackCredit = DbContext.Credits.FirstOrDefault(x => x.RoadieId == credit.Id);
                        if (trackCredit == null)
                        {
                            // Add new
                            trackCredit = new data.Credit
                            {
                                TrackId = track.Id,
                                CreatedDate = now
                            };
                            DbContext.Credits.Add(trackCredit);
                        }
                        data.Artist artistForCredit = null;
                        if (credit.Artist != null)
                        {
                            artistForCredit = await GetArtist(credit.Artist.Id).ConfigureAwait(false);
                        }
                        var creditCategory = DbContext.CreditCategory.FirstOrDefault(x => x.RoadieId.ToString() == credit.Category.Value);
                        trackCredit.CreditCategoryId = creditCategory.Id;
                        trackCredit.ArtistId = artistForCredit == null ? null : (int?)artistForCredit.Id;
                        trackCredit.IsLocked = credit.IsLocked;
                        trackCredit.Status = SafeParser.ToEnum<Statuses>(credit.Status);
                        trackCredit.CreditToName = artistForCredit == null ? credit.CreditName : null;
                        trackCredit.Description = credit.Description;
                        trackCredit.URLs = credit.URLs;
                        trackCredit.Tags = credit.Tags;
                        trackCredit.LastUpdated = now;
                    }
                }

                var trackImage = ImageHelper.ImageDataFromUrl(model.NewThumbnailData);
                if (trackImage != null)
                {
                    // Save unaltered image to cover file
                    var trackThumbnailName = track.PathToTrackThumbnail(Configuration);
                    File.WriteAllBytes(trackThumbnailName, ImageHelper.ConvertToJpegFormat(trackImage));
                }

                // See if Title was changed if so then  modify DB Filename and rename track
                var shouldFileNameBeUpdated = originalTitle != track.Title || originalTrackNumber != track.TrackNumber;
                if (shouldFileNameBeUpdated)
                {
                    track.FileName = FolderPathHelper.TrackFileName(Configuration, track.Title, track.TrackNumber, track.ReleaseMedia.MediaNumber, track.ReleaseMedia.TrackCount);
                    File.Move(originalFilename, track.PathToTrack(Configuration));
                }
                track.LastUpdated = now;
                await DbContext.SaveChangesAsync().ConfigureAwait(false);

                var trackFileInfo = new FileInfo(track.PathToTrack(Configuration));
                var audioMetaData = await AudioMetaDataHelper.GetInfo(trackFileInfo).ConfigureAwait(false);
                if (audioMetaData != null)
                {
                    audioMetaData.Title = track.Title;
                    if (trackArtist != null)
                    {
                        audioMetaData.Artist = trackArtist.Name;
                    }
                    AudioMetaDataHelper.WriteTags(audioMetaData, trackFileInfo);
                }
                CacheManager.ClearRegion(track.CacheRegion);
                Logger.LogInformation($"UpdateTrack `{track}` By User `{user}`");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                errors.Add(ex);
            }

            sw.Stop();

            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = errors.Count == 0,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }
    }
}