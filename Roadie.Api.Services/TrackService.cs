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

        public TrackService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext dbContext,
                             ICacheManager cacheManager,
                             ILogger<TrackService> logger,
                             IBookmarkService bookmarkService)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
            this.BookmarkService = bookmarkService;
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
                var track = this.GetTrack(id);
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
            result.PlayUrl = $"{ this.HttpContext.BaseUrl }/play/track/{track.RoadieId}.mp3";
            result.IsLocked = (track.IsLocked ?? false) || 
                              (track.ReleaseMedia.IsLocked ?? false) || 
                              (track.ReleaseMedia.Release.IsLocked ?? false ) || 
                              (track.ReleaseMedia.Release.Artist.IsLocked ?? false);
            result.Thumbnail = base.MakeTrackThumbnailImage(id);
            result.ReleaseMediaId = track.ReleaseMedia.RoadieId.ToString();
            result.Artist = new DataToken
            {
                Text = track.ReleaseMedia.Release.Artist.Name,
                Value = track.ReleaseMedia.Release.Artist.RoadieId.ToString()
            };
            result.ArtistThumbnail = this.MakeArtistThumbnailImage(track.ReleaseMedia.Release.Artist.RoadieId);
            result.Release = new DataToken
            {
                Text = track.ReleaseMedia.Release.Title,
                Value = track.ReleaseMedia.Release.RoadieId.ToString()
            };
            result.ReleaseThumbnail = this.MakeReleaseThumbnailImage(track.ReleaseMedia.Release.RoadieId);        
            if(track.ArtistId.HasValue)
            {
                var trackArtist = this.DbContext.Artists.FirstOrDefault(x => x.Id == track.ArtistId);
                if(trackArtist == null)
                {
                    this.Logger.LogWarning($"Unable to find Track Artist [{ track.ArtistId }");
                } 
                else
                {
                    result.TrackArtist = new DataToken
                    {
                        Text = trackArtist.Name,
                        Value = trackArtist.RoadieId.ToString()
                    };
                    result.TrackArtistThumbnail = this.MakeArtistThumbnailImage(trackArtist.RoadieId);
                }                
            }
            if (includes != null && includes.Any())
            {
                if (includes.Contains("stats"))
                {
                    var userTracks = (from t in this.DbContext.Tracks
                                      join ut in this.DbContext.UserTracks on t.Id equals ut.TrackId into tt
                                      from ut in tt.DefaultIfEmpty()
                                      where t.Id == track.Id
                                      select ut).ToArray();
                    if (userTracks.Any())
                    {
                        result.Statistics = new Library.Models.Statistics.TrackStatistics
                        {
                            DislikedCount = userTracks.Count(x => x.IsDisliked ?? false),
                            FavoriteCount = userTracks.Count(x => x.IsFavorite ?? false),
                            PlayedCount = userTracks.Sum(x => x.PlayedCount),
                            FileSizeFormatted = ((long?)track.FileSize).ToFileSize(),                            
                            Time = new TimeInfo((decimal)track.Duration).ToFullFormattedString()
                        };
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

        public Task<Library.Models.Pagination.PagedResult<TrackList>> List(PagedRequest request, User roadieUser, bool? doRandomize = false, Guid? releaseId = null)
        {
            try
            {


                var sw = new Stopwatch();
                sw.Start();

                int? rowCount = null;

                if(!string.IsNullOrEmpty(request.Sort))
                {
                    request.Sort = request.Sort.Replace("Release.Text", "Release.Release.Text");
                }

                IQueryable<int> favoriteTrackIds = (new int[0]).AsQueryable();
                if (request.FilterFavoriteOnly)
                {
                    favoriteTrackIds = (from t in this.DbContext.Tracks
                                        join ut in this.DbContext.UserTracks on t.Id equals ut.TrackId
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
                    var randomLimit = roadieUser?.RandomReleaseLimit ?? 50;
                    randomLimit = request.LimitValue > randomLimit ? randomLimit : request.LimitValue;
                    var sql = "SELECT t.* FROM `track` t WHERE t.Hash IS NOT NULL ORDER BY RAND() LIMIT {0}";
                    randomTrackIds = this.DbContext.Tracks.FromSql(sql, randomLimit).Select(x => x.Id).ToArray();
                    rowCount = this.DbContext.Tracks.Where(x => x.Hash != null).Count();
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
                                   where (request.FilterValue == "" || (t.Title.Contains(request.FilterValue) || t.AlternateNames.Contains(request.FilterValue)))
                                   where (!request.FilterFavoriteOnly || favoriteTrackIds.Contains(t.Id))
                                   where (request.FilterToPlaylistId == null || playlistTrackIds.Contains(t.Id))
                                   where (!request.FilterTopPlayedOnly || topTrackids.Contains(t.Id))
                                   where (randomTrackIds == null || randomTrackIds.Contains(t.Id))
                                   where (request.FilterToArtistId == null || request.FilterToArtistId != null && r.Artist.RoadieId == request.FilterToArtistId)
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
                                  Release = x.rl,
                                  LastPlayed = x.ti.LastPlayed,
                                  Artist = x.ra,
                                  TrackArtist = x.ta,
                                  TrackNumber = playListTrackPositions.ContainsKey(x.ti.Id) ? playListTrackPositions[x.ti.Id] : x.ti.TrackNumber,
                                  MediaNumber = x.rmi.MediaNumber,
                                  CreatedDate = x.ti.CreatedDate,
                                  LastUpdated = x.ti.LastUpdated,
                                  Duration = x.ti.Duration,
                                  FileSize = x.ti.FileSize,
                                  ReleaseDate = x.rl.ReleaseDateDateTime,
                                  PlayedCount = x.ti.PlayedCount,
                                  Rating = x.ti.Rating,
                                  Title = x.ti.Title,
                                  TrackPlayUrl = $"{ this.HttpContext.BaseUrl }/play/track/{ x.ti.RoadieId }.mp3",
                                  Thumbnail = this.MakeTrackThumbnailImage(x.ti.RoadieId)
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
                rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
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

                    foreach(var userReleaseRating in userReleaseRatings)
                    {
                        foreach(var row in rows.Where(x => x.Release.DatabaseId == userReleaseRating.ReleaseId))
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

        public async Task<OperationResult<TrackStreamInfo>> TrackStreamInfo(Guid trackId, long beginBytes, long endBytes)
        {
            var track = this.GetTrack(trackId);
            if (track == null)
            {
                return new OperationResult<TrackStreamInfo>($"TrackStreamInfo: Unable To Find Track [{ trackId }]");
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
                track.UpdateTrackMissingFile();
                await this.DbContext.SaveChangesAsync();
                return new OperationResult<TrackStreamInfo>($"TrackStreamInfo: TrackId [{trackId}] Unable to Find Track [{trackFileInfo.FullName}]");
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
    }
}