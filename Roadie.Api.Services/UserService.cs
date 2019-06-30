using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Identity;
using Roadie.Library.Imaging;
using Roadie.Library.MetaData.LastFm;
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
using models = Roadie.Library.Models;

namespace Roadie.Api.Services
{
    public class UserService : ServiceBase, IUserService
    {
        private ILastFmHelper LastFmHelper { get; }

        private UserManager<ApplicationUser> UserManager { get; }

        public UserService(IRoadieSettings configuration,
                            IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            data.IRoadieDbContext context,
            ICacheManager cacheManager,
            ILogger<ArtistService> logger,
            UserManager<ApplicationUser> userManager
        )
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
            UserManager = userManager;
            LastFmHelper = new LastFmHelper(Configuration, CacheManager, Logger, context, httpEncoder);
            ;
        }

        public async Task<OperationResult<User>> ById(User user, Guid id, IEnumerable<string> includes)
        {
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = string.Format("urn:user_by_id_operation:{0}", id);
            var result = await CacheManager.GetAsync(cacheKey, async () =>
            {
                tsw.Restart();
                var rr = await UserByIdAction(id, includes);
                tsw.Stop();
                timings.Add("UserByIdAction", tsw.ElapsedMilliseconds);
                return rr;
            }, ApplicationUser.CacheRegionUrn(id));
            sw.Stop();
            if (result?.Data != null) result.Data.Avatar = MakeUserThumbnailImage(id);
            timings.Add("operation", sw.ElapsedMilliseconds);
            Logger.LogDebug("ById Timings: id [{0}]", id);
            return new OperationResult<User>(result.Messages)
            {
                Data = result?.Data,
                Errors = result?.Errors,
                IsNotFoundResult = result?.IsNotFoundResult ?? false,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public Task<Library.Models.Pagination.PagedResult<UserList>> List(PagedRequest request)
        {
            var sw = new Stopwatch();
            sw.Start();

            var result = from u in DbContext.Users
                         let lastActivity = (from ut in DbContext.UserTracks
                                             where ut.UserId == u.Id
                                             select ut.LastPlayed).Max()
                         where request.FilterValue.Length == 0 ||
                               request.FilterValue.Length > 0 && u.UserName.Contains(request.FilterValue)
                         select new UserList
                         {
                             DatabaseId = u.Id,
                             Id = u.RoadieId,
                             User = new models.DataToken
                             {
                                 Text = u.UserName,
                                 Value = u.RoadieId.ToString()
                             },
                             IsEditor = u.UserRoles.Any(x => x.Role.Name == "Editor"),
                             IsPrivate = u.IsPrivate,
                             Thumbnail = MakeUserThumbnailImage(u.RoadieId),
                             CreatedDate = u.CreatedDate,
                             LastUpdated = u.LastUpdated,
                             RegisteredDate = u.RegisteredOn,
                             LastLoginDate = u.LastLogin,
                             LastApiAccessDate = u.LastApiAccess,
                             LastActivity = lastActivity
                         };

            UserList[] rows = null;
            var rowCount = result.Count();
            var sortBy = string.IsNullOrEmpty(request.Sort)
                ? request.OrderValue(new Dictionary<string, string> { { "User.Text", "ASC" } })
                : request.OrderValue();
            rows = result.OrderBy(sortBy).Skip(request.SkipValue).Take(request.LimitValue).ToArray();

            if (rows.Any())
                foreach (var row in rows)
                {
                    var userArtists = DbContext.UserArtists.Include(x => x.Artist)
                        .Where(x => x.UserId == row.DatabaseId).ToArray();
                    var userReleases = DbContext.UserReleases.Include(x => x.Release)
                        .Where(x => x.UserId == row.DatabaseId).ToArray();
                    var userTracks = DbContext.UserTracks.Include(x => x.Track).Where(x => x.UserId == row.DatabaseId)
                        .ToArray();

                    row.Statistics = new UserStatistics
                    {
                        RatedArtists = userArtists.Where(x => x.Rating > 0).Count(),
                        FavoritedArtists = userArtists.Where(x => x.IsFavorite ?? false).Count(),
                        DislikedArtists = userArtists.Where(x => x.IsDisliked ?? false).Count(),
                        RatedReleases = userReleases.Where(x => x.Rating > 0).Count(),
                        FavoritedReleases = userReleases.Where(x => x.IsFavorite ?? false).Count(),
                        DislikedReleases = userReleases.Where(x => x.IsDisliked ?? false).Count(),
                        RatedTracks = userTracks.Where(x => x.Rating > 0).Count(),
                        PlayedTracks = userTracks.Where(x => x.PlayedCount.HasValue).Select(x => x.PlayedCount).Sum(),
                        FavoritedTracks = userTracks.Where(x => x.IsFavorite ?? false).Count(),
                        DislikedTracks = userTracks.Where(x => x.IsDisliked ?? false).Count()
                    };
                }

            sw.Stop();
            return Task.FromResult(new Library.Models.Pagination.PagedResult<UserList>
            {
                TotalCount = rowCount,
                CurrentPage = request.PageValue,
                TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                OperationTime = sw.ElapsedMilliseconds,
                Rows = rows
            });
        }

        public async Task<OperationResult<bool>> SetArtistBookmark(Guid artistId, User roadieUser, bool isBookmarked)
        {
            var user = GetUser(roadieUser.UserId);
            if (user == null) return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            var artist = GetArtist(artistId);
            if (artist == null) return new OperationResult<bool>(true, $"Invalid Artist [{artistId}]");
            var result = await SetBookmark(user, BookmarkType.Artist, artist.Id, isBookmarked);

            CacheManager.ClearRegion(artist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        public async Task<OperationResult<bool>> SetArtistDisliked(Guid artistId, User roadieUser, bool isDisliked)
        {
            var user = GetUser(roadieUser.UserId);
            if (user == null) return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            return await ToggleArtistDisliked(artistId, user, isDisliked);
        }

        public async Task<OperationResult<bool>> SetArtistFavorite(Guid artistId, User roadieUser, bool isFavorite)
        {
            var user = GetUser(roadieUser.UserId);
            if (user == null) return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            return await ToggleArtistFavorite(artistId, user, isFavorite);
        }

        public async Task<OperationResult<short>> SetArtistRating(Guid artistId, User roadieUser, short rating)
        {
            var user = GetUser(roadieUser.UserId);
            if (user == null) return new OperationResult<short>(true, $"Invalid User [{roadieUser}]");
            return await base.SetArtistRating(artistId, user, rating);
        }

        public async Task<OperationResult<bool>> SetCollectionBookmark(Guid collectionId, User roadieUser,
            bool isBookmarked)
        {
            var user = GetUser(roadieUser.UserId);
            if (user == null) return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            var collection = GetCollection(collectionId);
            if (collection == null) return new OperationResult<bool>(true, $"Invalid Collection [{collectionId}]");
            var result = await SetBookmark(user, BookmarkType.Collection, collection.Id, isBookmarked);

            CacheManager.ClearRegion(collection.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        public async Task<OperationResult<bool>> SetLabelBookmark(Guid labelId, User roadieUser, bool isBookmarked)
        {
            var user = GetUser(roadieUser.UserId);
            if (user == null) return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            var label = GetLabel(labelId);
            if (label == null) return new OperationResult<bool>(true, $"Invalid Label [{labelId}]");
            var result = await SetBookmark(user, BookmarkType.Label, label.Id, isBookmarked);

            CacheManager.ClearRegion(label.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        public async Task<OperationResult<bool>> SetPlaylistBookmark(Guid playlistId, User roadieUser,
            bool isBookmarked)
        {
            var user = GetUser(roadieUser.UserId);
            if (user == null) return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            var playlist = GetPlaylist(playlistId);
            if (playlist == null) return new OperationResult<bool>(true, $"Invalid Playlist [{playlistId}]");
            var result = await SetBookmark(user, BookmarkType.Playlist, playlist.Id, isBookmarked);

            CacheManager.ClearRegion(playlist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        public async Task<OperationResult<bool>> SetReleaseBookmark(Guid releaseid, User roadieUser, bool isBookmarked)
        {
            var user = GetUser(roadieUser.UserId);
            if (user == null) return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            var release = GetRelease(releaseid);
            if (release == null) return new OperationResult<bool>(true, $"Invalid Release [{releaseid}]");
            var result = await SetBookmark(user, BookmarkType.Release, release.Id, isBookmarked);

            CacheManager.ClearRegion(release.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        public async Task<OperationResult<bool>> SetReleaseDisliked(Guid releaseId, User roadieUser, bool isDisliked)
        {
            var user = GetUser(roadieUser.UserId);
            if (user == null) return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            return await ToggleReleaseDisliked(releaseId, user, isDisliked);
        }

        public async Task<OperationResult<bool>> SetReleaseFavorite(Guid releaseId, User roadieUser, bool isFavorite)
        {
            var user = GetUser(roadieUser.UserId);
            if (user == null) return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            return await ToggleReleaseFavorite(releaseId, user, isFavorite);
        }

        public async Task<OperationResult<short>> SetReleaseRating(Guid releaseId, User roadieUser, short rating)
        {
            var user = GetUser(roadieUser.UserId);
            if (user == null) return new OperationResult<short>(true, $"Invalid User [{roadieUser}]");
            return await base.SetReleaseRating(releaseId, user, rating);
        }

        public async Task<OperationResult<bool>> SetTrackBookmark(Guid trackId, User roadieUser, bool isBookmarked)
        {
            var user = GetUser(roadieUser.UserId);
            if (user == null) return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            var track = GetTrack(trackId);
            if (track == null) return new OperationResult<bool>(true, $"Invalid Track [{trackId}]");
            var result = await SetBookmark(user, BookmarkType.Track, track.Id, isBookmarked);

            CacheManager.ClearRegion(track.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        public async Task<OperationResult<bool>> SetTrackDisliked(Guid trackId, User roadieUser, bool isDisliked)
        {
            var user = GetUser(roadieUser.UserId);
            if (user == null) return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            return await ToggleTrackDisliked(trackId, user, isDisliked);
        }

        public async Task<OperationResult<bool>> SetTrackFavorite(Guid trackId, User roadieUser, bool isFavorite)
        {
            var user = GetUser(roadieUser.UserId);
            if (user == null) return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            return await ToggleTrackFavorite(trackId, user, isFavorite);
        }

        public async Task<OperationResult<short>> SetTrackRating(Guid trackId, User roadieUser, short rating)
        {
            var timings = new Dictionary<string, long>();
            var sw = Stopwatch.StartNew();
            var user = GetUser(roadieUser.UserId);
            sw.Stop();
            timings.Add("GetUser", sw.ElapsedMilliseconds);

            if (user == null) return new OperationResult<short>(true, $"Invalid User [{roadieUser}]");
            sw.Start();
            var result = await base.SetTrackRating(trackId, user, rating);
            sw.Stop();
            timings.Add("SetTrackRating", sw.ElapsedMilliseconds);

            result.AdditionalData.Add("Timing", sw.ElapsedMilliseconds);
            Logger.LogInformation(
                $"User `{roadieUser}` set rating [{rating}] on TrackId [{trackId}]. Result [{JsonConvert.SerializeObject(result)}]");
            return result;
        }

        public async Task<OperationResult<bool>> UpdateIntegrationGrant(Guid userId, string integrationName,
            string token)
        {
            var user = DbContext.Users.FirstOrDefault(x => x.RoadieId == userId);
            if (user == null) return new OperationResult<bool>(true, $"User Not Found [{userId}]");
            if (integrationName == "lastfm") return await UpdateLastFMSessionKey(user, token);
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> UpdateProfile(User userPerformingUpdate, User userBeingUpdatedModel)
        {
            var user = DbContext.Users.FirstOrDefault(x => x.RoadieId == userBeingUpdatedModel.UserId);
            if (user == null)
                return new OperationResult<bool>(true,
                    string.Format("User Not Found [{0}]", userBeingUpdatedModel.UserId));
            if (user.Id != userPerformingUpdate.Id && !userPerformingUpdate.IsAdmin)
                return new OperationResult<bool>
                {
                    Errors = new List<Exception> { new Exception("Access Denied") }
                };
            // Check concurrency stamp
            if (user.ConcurrencyStamp != userBeingUpdatedModel.ConcurrencyStamp)
                return new OperationResult<bool>
                {
                    Errors = new List<Exception> { new Exception("User data is stale.") }
                };
            // Check that username (if changed) doesn't already exist
            if (user.UserName != userBeingUpdatedModel.UserName)
            {
                var userByUsername = DbContext.Users.FirstOrDefault(x =>
                    x.NormalizedUserName == userBeingUpdatedModel.UserName.ToUpper());
                if (userByUsername != null)
                    return new OperationResult<bool>
                    {
                        Errors = new List<Exception> { new Exception("Username already in use") }
                    };
            }

            // Check that email (if changed) doesn't already exist
            if (user.Email != userBeingUpdatedModel.Email)
            {
                var userByEmail =
                    DbContext.Users.FirstOrDefault(x => x.NormalizedEmail == userBeingUpdatedModel.Email.ToUpper());
                if (userByEmail != null)
                    return new OperationResult<bool>
                    {
                        Errors = new List<Exception> { new Exception("Email already in use") }
                    };
            }

            user.UserName = userBeingUpdatedModel.UserName;
            user.NormalizedUserName = userBeingUpdatedModel.UserName.ToUpper();
            user.Email = userBeingUpdatedModel.Email;
            user.NormalizedEmail = userBeingUpdatedModel.Email.ToUpper();
            user.ApiToken = userBeingUpdatedModel.ApiToken;
            user.Timezone = userBeingUpdatedModel.Timezone;
            user.Timeformat = userBeingUpdatedModel.Timeformat;
            user.PlayerTrackLimit = userBeingUpdatedModel.PlayerTrackLimit;
            user.RandomReleaseLimit = userBeingUpdatedModel.RandomReleaseLimit;
            user.RecentlyPlayedLimit = userBeingUpdatedModel.RecentlyPlayedLimit;
            user.Profile = userBeingUpdatedModel.Profile;
            user.DoUseHtmlPlayer = userBeingUpdatedModel.DoUseHtmlPlayer;
            user.RemoveTrackFromQueAfterPlayed = userBeingUpdatedModel.RemoveTrackFromQueAfterPlayed;
            user.IsPrivate = userBeingUpdatedModel.IsPrivate;
            user.LastUpdated = DateTime.UtcNow;
            user.FtpUrl = userBeingUpdatedModel.FtpUrl;
            user.FtpDirectory = userBeingUpdatedModel.FtpDirectory;
            user.FtpUsername = userBeingUpdatedModel.FtpUsername;
            user.FtpPassword = EncryptionHelper.Encrypt(userBeingUpdatedModel.FtpPassword, user.RoadieId.ToString());
            user.ConcurrencyStamp = Guid.NewGuid().ToString();

            if (!string.IsNullOrEmpty(userBeingUpdatedModel.AvatarData))
            {
                var imageData = ImageHelper.ImageDataFromUrl(userBeingUpdatedModel.AvatarData);
                if (imageData != null)
                    user.Avatar = ImageHelper.ResizeImage(imageData, Configuration.ThumbnailImageSize.Width,
                        Configuration.ThumbnailImageSize.Height);
            }

            await DbContext.SaveChangesAsync();

            if (!string.IsNullOrEmpty(userBeingUpdatedModel.Password) &&
                !string.IsNullOrEmpty(userBeingUpdatedModel.PasswordConfirmation))
            {
                if (userBeingUpdatedModel.Password != userBeingUpdatedModel.PasswordConfirmation)
                    return new OperationResult<bool>
                    {
                        Errors = new List<Exception> { new Exception("Password does not match confirmation") }
                    };
                var resetToken = await UserManager.GeneratePasswordResetTokenAsync(user);
                var identityResult =
                    await UserManager.ResetPasswordAsync(user, resetToken, userBeingUpdatedModel.Password);
                if (!identityResult.Succeeded)
                    return new OperationResult<bool>
                    {
                        Errors = identityResult.Errors != null
                            ? identityResult.Errors.Select(x =>
                                new Exception($"Code [{x.Code}], Description [{x.Description}]"))
                            : new List<Exception> { new Exception("Unable to reset password") }
                    };
            }

            CacheManager.ClearRegion(ApplicationUser.CacheRegionUrn(user.RoadieId));

            Logger.LogInformation($"User `{userPerformingUpdate}` modifed user `{userBeingUpdatedModel}`");

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        private async Task<OperationResult<bool>> SetBookmark(ApplicationUser user, BookmarkType bookmarktype,
            int bookmarkTargetId, bool isBookmarked)
        {
            var bookmark = DbContext.Bookmarks.FirstOrDefault(x => x.BookmarkTargetId == bookmarkTargetId &&
                                                                   x.BookmarkType == bookmarktype &&
                                                                   x.UserId == user.Id);
            if (!isBookmarked)
            {
                // Remove bookmark
                if (bookmark != null)
                {
                    DbContext.Bookmarks.Remove(bookmark);
                    await DbContext.SaveChangesAsync();
                }
            }
            else
            {
                // Add bookmark
                if (bookmark == null)
                {
                    DbContext.Bookmarks.Add(new data.Bookmark
                    {
                        UserId = user.Id,
                        BookmarkTargetId = bookmarkTargetId,
                        BookmarkType = bookmarktype,
                        CreatedDate = DateTime.UtcNow,
                        Status = Statuses.Ok
                    });
                    await DbContext.SaveChangesAsync();
                }
            }

            CacheManager.ClearRegion(user.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        private async Task<OperationResult<bool>> UpdateLastFMSessionKey(ApplicationUser user, string token)
        {
            var lastFmSessionKeyResult = await LastFmHelper.GetSessionKeyForUserToken(token);
            if (!lastFmSessionKeyResult.IsSuccess)
                return new OperationResult<bool>(false, $"Unable to Get LastFM Session Key For Token [{token}]");
            // Check concurrency stamp
            if (user.ConcurrencyStamp != user.ConcurrencyStamp)
                return new OperationResult<bool>
                {
                    Errors = new List<Exception> { new Exception("User data is stale.") }
                };
            user.LastFMSessionKey = lastFmSessionKeyResult.Data;
            user.LastUpdated = DateTime.UtcNow;
            user.ConcurrencyStamp = Guid.NewGuid().ToString();
            await DbContext.SaveChangesAsync();

            CacheManager.ClearRegion(ApplicationUser.CacheRegionUrn(user.RoadieId));

            Logger.LogInformation($"User `{user}` Updated LastFm SessionKey");

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        private Task<OperationResult<User>> UserByIdAction(Guid id, IEnumerable<string> includes)
        {
            var user = GetUser(id);
            if (user == null)
                return Task.FromResult(new OperationResult<User>(true, string.Format("User Not Found [{0}]", id)));
            var model = user.Adapt<User>();
            if (includes != null && includes.Any())
                if (includes.Contains("stats"))
                {
                    var userArtists =
                        DbContext.UserArtists.Include(x => x.Artist).Where(x => x.UserId == user.Id).ToArray() ??
                        new data.UserArtist[0];
                    var userReleases =
                        DbContext.UserReleases.Include(x => x.Release).Where(x => x.UserId == user.Id).ToArray() ??
                        new data.UserRelease[0];
                    var userTracks =
                        DbContext.UserTracks.Include(x => x.Track).Where(x => x.UserId == user.Id).ToArray() ??
                        new data.UserTrack[0];

                    var mostPlayedArtist = (from a in DbContext.Artists
                                            join r in DbContext.Releases on a.Id equals r.ArtistId
                                            join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                            join t in DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                            join ut in DbContext.UserTracks on t.Id equals ut.TrackId
                                            where ut.UserId == user.Id
                                            select new { a, ut.PlayedCount })
                        .GroupBy(a => a.a)
                        .Select(x => new
                        {
                            Artist = x.Key,
                            Played = x.Sum(t => t.PlayedCount)
                        })
                        .OrderByDescending(x => x.Played)
                        .FirstOrDefault();

                    var mostPlayedReleaseId = (from r in DbContext.Releases
                                               join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                               join t in DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                               join ut in DbContext.UserTracks on t.Id equals ut.TrackId
                                               where ut.UserId == user.Id
                                               select new { r, ut.PlayedCount })
                        .GroupBy(r => r.r)
                        .Select(x => new
                        {
                            Release = x.Key,
                            Played = x.Sum(t => t.PlayedCount)
                        })
                        .OrderByDescending(x => x.Played)
                        .Select(x => x.Release.RoadieId)
                        .FirstOrDefault();

                    var mostPlayedRelease = GetRelease(mostPlayedReleaseId);

                    var mostPlayedTrackUserTrack = userTracks
                        .OrderByDescending(x => x.PlayedCount)
                        .FirstOrDefault();
                    var mostPlayedTrack = mostPlayedTrackUserTrack == null
                        ? null
                        : DbContext.Tracks
                            .Include(x => x.TrackArtist)
                            .Include(x => x.ReleaseMedia)
                            .Include("ReleaseMedia.Release")
                            .Include("ReleaseMedia.Release.Artist")
                            .FirstOrDefault(x => x.Id == mostPlayedTrackUserTrack.TrackId);

                    model.Statistics = new UserStatistics
                    {
                        MostPlayedArtist = mostPlayedArtist == null
                            ? null
                            : models.ArtistList.FromDataArtist(mostPlayedArtist.Artist,
                                MakeArtistThumbnailImage(mostPlayedArtist.Artist.RoadieId)),
                        MostPlayedRelease = mostPlayedRelease == null
                            ? null
                            : ReleaseList.FromDataRelease(mostPlayedRelease,
                                mostPlayedRelease.Artist,
                                HttpContext.BaseUrl,
                                MakeArtistThumbnailImage(mostPlayedRelease.Artist.RoadieId),
                                MakeReleaseThumbnailImage(mostPlayedRelease.RoadieId)),
                        MostPlayedTrack = mostPlayedTrack == null
                            ? null
                            : models.TrackList.FromDataTrack(
                                MakeTrackPlayUrl(user, mostPlayedTrack.Id, mostPlayedTrack.RoadieId),
                                mostPlayedTrack,
                                mostPlayedTrack.ReleaseMedia.MediaNumber,
                                mostPlayedTrack.ReleaseMedia.Release,
                                mostPlayedTrack.ReleaseMedia.Release.Artist,
                                mostPlayedTrack.TrackArtist,
                                HttpContext.BaseUrl,
                                MakeTrackThumbnailImage(mostPlayedTrack.RoadieId),
                                MakeReleaseThumbnailImage(mostPlayedTrack.ReleaseMedia.Release.RoadieId),
                                MakeArtistThumbnailImage(mostPlayedTrack.ReleaseMedia.Release.Artist.RoadieId),
                                MakeArtistThumbnailImage(mostPlayedTrack.TrackArtist == null
                                    ? null
                                    : (Guid?)mostPlayedTrack.TrackArtist.RoadieId)),
                        RatedArtists = userArtists.Where(x => x.Rating > 0).Count(),
                        FavoritedArtists = userArtists.Where(x => x.IsFavorite ?? false).Count(),
                        DislikedArtists = userArtists.Where(x => x.IsDisliked ?? false).Count(),
                        RatedReleases = userReleases.Where(x => x.Rating > 0).Count(),
                        FavoritedReleases = userReleases.Where(x => x.IsFavorite ?? false).Count(),
                        DislikedReleases = userReleases.Where(x => x.IsDisliked ?? false).Count(),
                        RatedTracks = userTracks.Where(x => x.Rating > 0).Count(),
                        PlayedTracks = userTracks.Where(x => x.PlayedCount.HasValue).Select(x => x.PlayedCount).Sum(),
                        FavoritedTracks = userTracks.Where(x => x.IsFavorite ?? false).Count(),
                        DislikedTracks = userTracks.Where(x => x.IsDisliked ?? false).Count()
                    };
                }

            return Task.FromResult(new OperationResult<User>
            {
                IsSuccess = true,
                Data = model
            });
        }
    }
}