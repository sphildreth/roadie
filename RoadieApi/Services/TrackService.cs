using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Extensions;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
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
        public TrackService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext dbContext,
                             ICacheManager cacheManager,
                             ILogger<TrackService> logger)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
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
                //var artist = this.GetArtist(id);
                //result.Data.UserBookmark = this.GetUserBookmarks(roadieUser).FirstOrDefault(x => x.Type == BookmarkType.Artist && x.Bookmark.Value == artist.RoadieId.ToString());
                //var userArtist = this.DbContext.UserArtists.FirstOrDefault(x => x.ArtistId == artist.Id && x.UserId == roadieUser.Id);
                //if (userArtist != null)
                //{
                //    result.Data.UserRating = new UserArtist
                //    {
                //        IsDisliked = userArtist.IsDisliked ?? false,
                //        IsFavorite = userArtist.IsFavorite ?? false,
                //        Rating = userArtist.Rating
                //    };
                //}

                //if (this.RoadieUser != null)
                //{
                //    var userTrack = context.usertracks.FirstOrDefault(x => x.trackId == trackInfo.t.id && x.userId == this.RoadieUser.id);
                //    if (userTrack != null)
                //    {
                //        result.UserTrack = Map.ObjectToObject<dto.UserTrack>(userTrack);
                //        result.UserTrack.userId = this.RoadieUser.roadieId;
                //        result.UserTrack.trackId = result.roadieId;
                //        result.UserTrack.createdDateTime = userTrack.createdDate;
                //        result.UserTrack.lastUpdatedDateTime = userTrack.lastUpdated;
                //    }
                //}

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

        private async Task<OperationResult<Track>> TrackByIdAction(Guid id, IEnumerable<string> includes)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();

            var track = this.GetTrack(id);

            if (track == null)
            {
                return new OperationResult<Track>(true, string.Format("Track Not Found [{0}]", id));
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
                            Time = TimeSpan.FromSeconds(Math.Floor((double)track.Duration / 1000)).ToString(@"hh\:mm\:ss")
                        };
                    }
                }
            }


            sw.Stop();
            return new OperationResult<Track>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            };
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

        public async Task<Library.Models.Pagination.PagedResult<TrackList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false, Guid? releaseId = null)
        {
            var sw = new Stopwatch();
            sw.Start();

            IQueryable<int> favoriteTrackIds = (new int[0]).AsQueryable();
            if (request.FilterFavoriteOnly)
            {
                favoriteTrackIds = (from t in this.DbContext.Tracks
                                     join ut in this.DbContext.UserTracks on t.Id equals ut.TrackId
                                     where ut.IsFavorite ?? false
                                     select t.Id
                                     );
            }
            IQueryable<int> playlistTrackIds = (new int[0]).AsQueryable();
            if(request.FilterToPlaylistId.HasValue)
            {
                playlistTrackIds = (from plt in this.DbContext.PlaylistTracks
                                    join p in this.DbContext.Playlists on plt.PlayListId equals p.Id
                                    join t in this.DbContext.Tracks on plt.TrackId equals t.Id
                                    where p.RoadieId == request.FilterToPlaylistId.Value
                                    select t.Id
                                    );
            }
            int[] topTrackids = new int[0];
            if(request.FilterTopPlayedOnly)
            {
                // Get request number of top played songs for artist
                topTrackids = (from t in this.DbContext.Tracks
                               join ut in this.DbContext.UserTracks on t.Id equals ut.TrackId
                               join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                               join r in this.DbContext.Releases on rm.ReleaseId equals r.Id
                               where r.RoadieId == request.FilterToArtistId
                               orderby ut.PlayedCount descending
                               select t.Id
                               ).Skip(request.SkipValue).Take(request.LimitValue).ToArray();
            }
            var resultQuery = (from t in this.DbContext.Tracks
                               join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                               join r in this.DbContext.Releases on rm.ReleaseId equals r.Id
                               join trackArtist in this.DbContext.Artists on t.ArtistId equals trackArtist.Id into tas
                               from trackArtist in tas.DefaultIfEmpty()
                               join releaseArtist in this.DbContext.Artists on r.ArtistId equals releaseArtist.Id into aa
                               from releaseArtist in aa.DefaultIfEmpty()
                               where (t.Hash != null)
                               where (releaseId == null || (releaseId != null && r.RoadieId == releaseId))
                               where (request.FilterToTrackId == null || request.FilterToTrackId != null && t.RoadieId == request.FilterToTrackId)
                               where (request.FilterMinimumRating == null || t.Rating >= request.FilterMinimumRating.Value)
                               where (request.FilterValue == "" || (t.Title.Contains(request.FilterValue) || t.AlternateNames.Contains(request.FilterValue)))
                               where (!request.FilterFavoriteOnly || favoriteTrackIds.Contains(t.Id))
                               where (request.FilterToPlaylistId == null || playlistTrackIds.Contains(t.Id))
                               where (!request.FilterTopPlayedOnly || topTrackids.Contains(t.Id))
                               where (request.FilterToArtistId == null || request.FilterToArtistId != null && r.Artist.RoadieId == request.FilterToArtistId)
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
                              FileSize = x.t.FileSize,
                              ReleaseDate = x.r.ReleaseDate,
                              Rating = x.t.Rating,
                              ArtistThumbnail = this.MakeArtistThumbnailImage(x.releaseArtist.RoadieId),
                              Title = x.t.Title,
                              TrackArtistThumbnail = x.trackArtist != null ? this.MakeArtistThumbnailImage(x.trackArtist.RoadieId) : null,
                              TrackPlayUrl = $"{ this.HttpContext.BaseUrl }/play/track/{ x.t.RoadieId }.mp3",
                              Thumbnail = this.MakeTrackThumbnailImage(x.t.RoadieId)
                          });
            string sortBy = null;

            var rowCount = result.Count();
            TrackList[] rows = null;

            if (doRandomize ?? false)
            {
                var randomLimit = roadieUser?.RandomReleaseLimit ?? 100;
                request.Limit = request.LimitValue > randomLimit ? randomLimit : request.LimitValue;
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
                Data = info
            };
        }
    }
}