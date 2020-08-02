using Mapster;
using Microsoft.AspNetCore.Identity;
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
using Roadie.Library.MetaData.LastFm;
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
using models = Roadie.Library.Models;

namespace Roadie.Api.Services
{
    public class UserService : ServiceBase, IUserService
    {
        private ILastFmHelper LastFmHelper { get; }

        private UserManager<Library.Identity.User> UserManager { get; }

        public UserService(IRoadieSettings configuration,
                            IHttpEncoder httpEncoder,
                            IHttpContext httpContext,
                            IRoadieDbContext context,
                            ICacheManager cacheManager,
                            ILogger<ArtistService> logger,
                            UserManager<Library.Identity.User> userManager,
                            ILastFmHelper lastFmHelper
        )
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
            UserManager = userManager;
            LastFmHelper = lastFmHelper;
        }

        private async Task<OperationResult<bool>> SetBookmark(Library.Identity.User user, BookmarkType bookmarktype, int bookmarkTargetId, bool isBookmarked)
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
                    await DbContext.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            else
            {
                // Add bookmark
                if (bookmark == null)
                {
                    await DbContext.Bookmarks.AddAsync(new data.Bookmark
                    {
                        UserId = user.Id,
                        BookmarkTargetId = bookmarkTargetId,
                        BookmarkType = bookmarktype,
                        CreatedDate = DateTime.UtcNow,
                        Status = Statuses.Ok
                    }).ConfigureAwait(false);
                    await DbContext.SaveChangesAsync().ConfigureAwait(false);
                }
            }

            CacheManager.ClearRegion(user.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        private async Task<OperationResult<bool>> UpdateLastFMSessionKey(Library.Identity.User user, string token)
        {
            var lastFmSessionKeyResult = await LastFmHelper.GetSessionKeyForUserToken(token).ConfigureAwait(false);
            if (!lastFmSessionKeyResult.IsSuccess)
            {
                return new OperationResult<bool>(false, $"Unable to Get LastFM Session Key For Token [{token}]");
            }
            // Check concurrency stamp
            if (user.ConcurrencyStamp != user.ConcurrencyStamp)
            {
                return new OperationResult<bool>
                {
                    Errors = new List<Exception> { new Exception("User data is stale.") }
                };
            }

            user.LastFMSessionKey = lastFmSessionKeyResult.Data;
            user.LastUpdated = DateTime.UtcNow;
            user.ConcurrencyStamp = Guid.NewGuid().ToString();
            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            CacheManager.ClearRegion(Library.Identity.User.CacheRegionUrn(user.RoadieId));

            Logger.LogTrace($"User `{user}` Updated LastFm SessionKey");

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        private async Task<OperationResult<User>> UserByIdAction(Guid id, IEnumerable<string> includes)
        {
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            tsw.Restart();
            var user = await GetUser(id).ConfigureAwait(false);
            tsw.Stop();
            timings.Add("getUser", tsw.ElapsedMilliseconds);

            if (user == null)
            {
                return new OperationResult<User>(true, $"User Not Found [{id}]");
            }
            tsw.Restart();
            var model = user.Adapt<User>();
            model.MediumThumbnail = ImageHelper.MakeThumbnailImage(Configuration, HttpContext, id, "user", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height);
            model.IsAdmin = user.UserRoles?.Any(x => x.Role?.NormalizedName == "ADMIN") ?? false;
            model.IsEditor = model.IsAdmin || (user.UserRoles?.Any(x => x.Role?.NormalizedName == "EDITOR") ?? false);
            tsw.Stop();
            timings.Add("adapt", tsw.ElapsedMilliseconds);

            if (includes?.Any() == true && includes.Contains("stats"))
            {
                tsw.Restart();
                var userArtists = DbContext.UserArtists.Include(x => x.Artist).Where(x => x.UserId == user.Id).ToArray() ?? new data.UserArtist[0];
                var userReleases = DbContext.UserReleases.Include(x => x.Release).Where(x => x.UserId == user.Id).ToArray() ?? new data.UserRelease[0];
                var userTracks = DbContext.UserTracks.Include(x => x.Track).Where(x => x.UserId == user.Id).ToArray() ?? new data.UserTrack[0];

                var mostPlayedArtist = await DbContext.MostPlayedArtist(user.Id).ConfigureAwait(false);
                var mostPlayedRelease = await DbContext.MostPlayedRelease(user.Id).ConfigureAwait(false);
                var lastPlayedTrack = await GetTrack((await DbContext.MostPlayedTrack(user.Id).ConfigureAwait(false))?.RoadieId ?? Guid.Empty).ConfigureAwait(false);
                var mostPlayedTrack = await GetTrack((await DbContext.LastPlayedTrack(user.Id).ConfigureAwait(false))?.RoadieId ?? Guid.Empty).ConfigureAwait(false);
                model.Statistics = new UserStatistics
                {
                    LastPlayedTrack = lastPlayedTrack == null
                        ? null
                        : models.TrackList.FromDataTrack(
                            MakeTrackPlayUrl(user, HttpContext.BaseUrl, lastPlayedTrack.RoadieId),
                            lastPlayedTrack,
                            lastPlayedTrack.ReleaseMedia.MediaNumber,
                            lastPlayedTrack.ReleaseMedia.Release,
                            lastPlayedTrack.ReleaseMedia.Release.Artist,
                            lastPlayedTrack.TrackArtist,
                            HttpContext.BaseUrl,
                            ImageHelper.MakeTrackThumbnailImage(Configuration, HttpContext, lastPlayedTrack.RoadieId),
                            ImageHelper.MakeReleaseThumbnailImage(Configuration, HttpContext, lastPlayedTrack.ReleaseMedia.Release.RoadieId),
                            ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, lastPlayedTrack.ReleaseMedia.Release.Artist.RoadieId),
                            ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, lastPlayedTrack.TrackArtist == null
                                ? null
                                : (Guid?)lastPlayedTrack.TrackArtist.RoadieId)),
                    MostPlayedArtist = mostPlayedArtist == null
                        ? null
                        : models.ArtistList.FromDataArtist(mostPlayedArtist,
                            ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, mostPlayedArtist.RoadieId)),
                    MostPlayedRelease = mostPlayedRelease == null
                        ? null
                        : ReleaseList.FromDataRelease(mostPlayedRelease,
                            mostPlayedRelease.Artist,
                            HttpContext.BaseUrl,
                            ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, mostPlayedRelease.Artist.RoadieId),
                            ImageHelper.MakeReleaseThumbnailImage(Configuration, HttpContext, mostPlayedRelease.RoadieId)),
                    MostPlayedTrack = mostPlayedTrack == null
                        ? null
                        : models.TrackList.FromDataTrack(
                            MakeTrackPlayUrl(user, HttpContext.BaseUrl, mostPlayedTrack.RoadieId),
                            mostPlayedTrack,
                            mostPlayedTrack.ReleaseMedia.MediaNumber,
                            mostPlayedTrack.ReleaseMedia.Release,
                            mostPlayedTrack.ReleaseMedia.Release.Artist,
                            mostPlayedTrack.TrackArtist,
                            HttpContext.BaseUrl,
                            ImageHelper.MakeTrackThumbnailImage(Configuration, HttpContext, mostPlayedTrack.RoadieId),
                            ImageHelper.MakeReleaseThumbnailImage(Configuration, HttpContext, mostPlayedTrack.ReleaseMedia.Release.RoadieId),
                            ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, mostPlayedTrack.ReleaseMedia.Release.Artist.RoadieId),
                            ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, mostPlayedTrack.TrackArtist == null
                                ? null
                                : (Guid?)mostPlayedTrack.TrackArtist.RoadieId)),
                    RatedArtists = userArtists.Count(x => x.Rating > 0),
                    FavoritedArtists = userArtists.Count(x => x.IsFavorite ?? false),
                    DislikedArtists = userArtists.Count(x => x.IsDisliked ?? false),
                    RatedReleases = userReleases.Count(x => x.Rating > 0),
                    FavoritedReleases = userReleases.Count(x => x.IsFavorite ?? false),
                    DislikedReleases = userReleases.Count(x => x.IsDisliked ?? false),
                    RatedTracks = userTracks.Count(x => x.Rating > 0),
                    PlayedTracks = userTracks.Where(x => x.PlayedCount.HasValue).Select(x => x.PlayedCount).Sum(),
                    FavoritedTracks = userTracks.Count(x => x.IsFavorite ?? false),
                    DislikedTracks = userTracks.Count(x => x.IsDisliked ?? false)
                };
                tsw.Stop();
                timings.Add("stats", tsw.ElapsedMilliseconds);
            }
            Logger.LogInformation($"ByIdAction: User `{ user }`: includes [{includes.ToCSV()}], timings: [{ timings.ToTimings() }]");
            return new OperationResult<User>
            {
                IsSuccess = true,
                Data = model
            };
        }

        public async Task<OperationResult<User>> ByIdAsync(User user, Guid id, IEnumerable<string> includes, bool isAccountSettingsEdit = false)
        {
            if (isAccountSettingsEdit && user.UserId != id && !user.IsAdmin)
            {
                return new OperationResult<User>("Access Denied")
                {
                    IsAccessDeniedResult = true
                };
            }
            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = $"urn:user_by_id_operation:{id}:{isAccountSettingsEdit}";
            var result = await CacheManager.GetAsync(cacheKey, async () => await UserByIdAction(id, includes).ConfigureAwait(false), Library.Identity.User.CacheRegionUrn(id)).ConfigureAwait(false);
            sw.Stop();
            if (result?.Data != null)
            {
                result.Data.Avatar = ImageHelper.MakeUserThumbnailImage(Configuration, HttpContext, id);
                if (!isAccountSettingsEdit)
                {
                    result.Data.ApiToken = null;
                    result.Data.ConcurrencyStamp = null;
                }
            }
            return new OperationResult<User>(result.Messages)
            {
                Data = result?.Data,
                Errors = result?.Errors,
                IsNotFoundResult = result?.IsNotFoundResult ?? false,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> DeleteAllBookmarksAsync(User roadieUser)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            }

            DbContext.Bookmarks.RemoveRange(DbContext.Bookmarks.Where(x => x.UserId == user.Id));
            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            CacheManager.ClearRegion(user.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        public Task<models.Pagination.PagedResult<UserList>> ListAsync(PagedRequest request)
        {
            var sw = new Stopwatch();
            sw.Start();

            var result = from u in DbContext.Users
                         let lastActivity = (from ut in DbContext.UserTracks
                                             where ut.UserId == u.Id
                                             select ut.LastPlayed).Max()
                         where request.FilterValue.Length == 0 ||
                               (request.FilterValue.Length > 0 && u.UserName.Contains(request.FilterValue))
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
                             IsAdmin = u.UserRoles.Any(x => x.Role.Name == "Admin"),
                             IsPrivate = u.IsPrivate,
                             Thumbnail = ImageHelper.MakeUserThumbnailImage(Configuration, HttpContext, u.RoadieId),
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

            if (rows.Length > 0)
            {
                foreach (var row in rows)
                {
                    var userArtists = DbContext.UserArtists.Include(x => x.Artist)
                                               .Where(x => x.UserId == row.DatabaseId)
                                               .ToArray();
                    var userReleases = DbContext.UserReleases.Include(x => x.Release)
                                                .Where(x => x.UserId == row.DatabaseId)
                                                .ToArray();
                    var userTracks = DbContext.UserTracks.Include(x => x.Track)
                                              .Where(x => x.UserId == row.DatabaseId)
                                              .ToArray();
                    row.Statistics = new UserStatistics
                    {
                        RatedArtists = userArtists.Count(x => x.Rating > 0),
                        FavoritedArtists = userArtists.Count(x => x.IsFavorite ?? false),
                        DislikedArtists = userArtists.Count(x => x.IsDisliked ?? false),
                        RatedReleases = userReleases.Count(x => x.Rating > 0),
                        FavoritedReleases = userReleases.Count(x => x.IsFavorite ?? false),
                        DislikedReleases = userReleases.Count(x => x.IsDisliked ?? false),
                        RatedTracks = userTracks.Count(x => x.Rating > 0),
                        PlayedTracks = userTracks.Where(x => x.PlayedCount.HasValue).Select(x => x.PlayedCount).Sum(),
                        FavoritedTracks = userTracks.Count(x => x.IsFavorite ?? false),
                        DislikedTracks = userTracks.Count(x => x.IsDisliked ?? false)
                    };
                }
            }

            sw.Stop();
            return Task.FromResult(new models.Pagination.PagedResult<UserList>
            {
                TotalCount = rowCount,
                CurrentPage = request.PageValue,
                TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                OperationTime = sw.ElapsedMilliseconds,
                Rows = rows
            });
        }

        public async Task<OperationResult<bool>> SetArtistBookmarkAsync(Guid artistId, User roadieUser, bool isBookmarked)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            }
            var artist = await GetArtist(artistId).ConfigureAwait(false);
            if (artist == null)
            {
                return new OperationResult<bool>(true, $"Invalid Artist [{artistId}]");
            }

            await SetBookmark(user, BookmarkType.Artist, artist.Id, isBookmarked).ConfigureAwait(false);

            CacheManager.ClearRegion(artist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        public async Task<OperationResult<bool>> SetArtistDislikedAsync(Guid artistId, User roadieUser, bool isDisliked)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            }
            return await ToggleArtistDisliked(artistId, user, isDisliked).ConfigureAwait(false);
        }

        public async Task<OperationResult<bool>> SetArtistFavoriteAsync(Guid artistId, User roadieUser, bool isFavorite)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            }
            return await ToggleArtistFavorite(artistId, user, isFavorite).ConfigureAwait(false);
        }

        public async Task<OperationResult<short>> SetArtistRatingAsync(Guid artistId, User roadieUser, short rating)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return new OperationResult<short>(true, $"Invalid User [{roadieUser}]");
            }
            return await SetArtistRating(artistId, user, rating).ConfigureAwait(false);
        }

        public async Task<OperationResult<bool>> SetCollectionBookmarkAsync(Guid collectionId, User roadieUser, bool isBookmarked)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            }
            var collection = await GetCollection(collectionId).ConfigureAwait(false);
            if (collection == null)
            {
                return new OperationResult<bool>(true, $"Invalid Collection [{collectionId}]");
            }
            await SetBookmark(user, BookmarkType.Collection, collection.Id, isBookmarked).ConfigureAwait(false);

            CacheManager.ClearRegion(collection.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        public async Task<OperationResult<bool>> SetLabelBookmarkAsync(Guid labelId, User roadieUser, bool isBookmarked)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            }
            var label = await GetLabel(labelId).ConfigureAwait(false);
            if (label == null)
            {
                return new OperationResult<bool>(true, $"Invalid Label [{labelId}]");
            }
            await SetBookmark(user, BookmarkType.Label, label.Id, isBookmarked).ConfigureAwait(false);

            CacheManager.ClearRegion(label.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        public async Task<OperationResult<bool>> SetPlaylistBookmarkAsync(Guid playlistId, User roadieUser,
            bool isBookmarked)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            }
            var playlist = await GetPlaylist(playlistId).ConfigureAwait(false);
            if (playlist == null)
            {
                return new OperationResult<bool>(true, $"Invalid Playlist [{playlistId}]");
            }
            await SetBookmark(user, BookmarkType.Playlist, playlist.Id, isBookmarked).ConfigureAwait(false);

            CacheManager.ClearRegion(playlist.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        public async Task<OperationResult<bool>> SetReleaseBookmarkAsync(Guid releaseid, User roadieUser, bool isBookmarked)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            }
            var release = await GetRelease(releaseid).ConfigureAwait(false);
            if (release == null)
            {
                return new OperationResult<bool>(true, $"Invalid Release [{releaseid}]");
            }
            await SetBookmark(user, BookmarkType.Release, release.Id, isBookmarked).ConfigureAwait(false);

            CacheManager.ClearRegion(release.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        public async Task<OperationResult<bool>> SetReleaseDislikedAsync(Guid releaseId, User roadieUser, bool isDisliked)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            }
            return await ToggleReleaseDisliked(releaseId, user, isDisliked).ConfigureAwait(false);
        }

        public async Task<OperationResult<bool>> SetReleaseFavoriteAsync(Guid releaseId, User roadieUser, bool isFavorite)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            }
            return await ToggleReleaseFavorite(releaseId, user, isFavorite).ConfigureAwait(false);
        }

        public async Task<OperationResult<short>> SetReleaseRatingAsync(Guid releaseId, User roadieUser, short rating)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return new OperationResult<short>(true, $"Invalid User [{roadieUser}]");
            }
            return await SetReleaseRating(releaseId, user, rating).ConfigureAwait(false);
        }

        public async Task<OperationResult<bool>> SetTrackBookmarkAsync(Guid trackId, User roadieUser, bool isBookmarked)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            }
            var track = await GetTrack(trackId).ConfigureAwait(false);
            if (track == null)
            {
                return new OperationResult<bool>(true, $"Invalid Track [{trackId}]");
            }
            await SetBookmark(user, BookmarkType.Track, track.Id, isBookmarked).ConfigureAwait(false);

            CacheManager.ClearRegion(track.CacheRegion);

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }

        public async Task<OperationResult<bool>> SetTrackDislikedAsync(Guid trackId, User roadieUser, bool isDisliked)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            }
            return await ToggleTrackDisliked(trackId, user, isDisliked).ConfigureAwait(false);
        }

        public async Task<OperationResult<bool>> SetTrackFavoriteAsync(Guid trackId, User roadieUser, bool isFavorite)
        {
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return new OperationResult<bool>(true, $"Invalid User [{roadieUser}]");
            }
            return await ToggleTrackFavorite(trackId, user, isFavorite).ConfigureAwait(false);
        }

        public async Task<OperationResult<short>> SetTrackRatingAsync(Guid trackId, User roadieUser, short rating)
        {
            var timings = new Dictionary<string, long>();
            var sw = Stopwatch.StartNew();
            var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
            sw.Stop();
            timings.Add(nameof(GetUser), sw.ElapsedMilliseconds);

            if (user == null)
            {
                return new OperationResult<short>(true, $"Invalid User [{roadieUser}]");
            }
            sw.Start();
            var result = await SetTrackRating(trackId, user, rating).ConfigureAwait(false);
            sw.Stop();
            timings.Add(nameof(SetTrackRatingAsync), sw.ElapsedMilliseconds);

            result.AdditionalData.Add("Timing", sw.ElapsedMilliseconds);
            Logger.LogTrace(
                $"User `{roadieUser}` set rating [{rating}] on TrackId [{trackId}]. Result [{CacheManager.CacheSerializer.Serialize(result)}]");
            return result;
        }

        public async Task<OperationResult<bool>> UpdateIntegrationGrantAsync(Guid userId, string integrationName, string token)
        {
            var user = DbContext.Users.FirstOrDefault(x => x.RoadieId == userId);
            if (user == null)
            {
                return new OperationResult<bool>(true, $"User Not Found [{userId}]");
            }
            if (integrationName == "lastfm")
            {
                return await UpdateLastFMSessionKey(user, token).ConfigureAwait(false);
            }
            throw new NotImplementedException();
        }

        public async Task<OperationResult<bool>> UpdateProfileAsync(User userPerformingUpdate, User userBeingUpdatedModel)
        {
            var user = DbContext.Users.FirstOrDefault(x => x.RoadieId == userBeingUpdatedModel.UserId);
            if (user == null)
            {
                return new OperationResult<bool>(true, $"User Not Found [{userBeingUpdatedModel.UserId}]");
            }
            if (user.Id != userPerformingUpdate.Id && !userPerformingUpdate.IsAdmin)
            {
                return new OperationResult<bool>("Access Denied")
                {
                    IsAccessDeniedResult = true
                };
            }
            // Check concurrency stamp
            if (user.ConcurrencyStamp != userBeingUpdatedModel.ConcurrencyStamp)
            {
                return new OperationResult<bool>("User data is stale.");
            }
            // Check that username (if changed) doesn't already exist
            if (user.UserName != userBeingUpdatedModel.UserName)
            {
                var userByUsername = DbContext.Users.FirstOrDefault(x => x.NormalizedUserName == userBeingUpdatedModel.UserName.ToUpper());
                if (userByUsername != null)
                {
                    return new OperationResult<bool>("Username already in use");
                }
            }

            // Check that email (if changed) doesn't already exist
            if (user.Email != userBeingUpdatedModel.Email)
            {
                var userByEmail = DbContext.Users.FirstOrDefault(x => x.NormalizedEmail == userBeingUpdatedModel.Email.ToUpper());
                if (userByEmail != null)
                {
                    return new OperationResult<bool>("Email already in use");
                }
            }
            var oldPathToImage = user.PathToImage(Configuration);
            var didChangeName = user.UserName != userBeingUpdatedModel.UserName;
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
            user.DefaultRowsPerPage = userBeingUpdatedModel.DefaultRowsPerPage;

            if (didChangeName && File.Exists(oldPathToImage))
            {
                File.Move(oldPathToImage, user.PathToImage(Configuration));
            }

            if (!string.IsNullOrEmpty(userBeingUpdatedModel.AvatarData))
            {
                var imageData = ImageHelper.ImageDataFromUrl(userBeingUpdatedModel.AvatarData);
                if (imageData != null)
                {
                    imageData = ImageHelper.ConvertToGifFormat(imageData);

                    // Save unaltered user image
                    File.WriteAllBytes(user.PathToImage(Configuration, true), imageData);
                }
            }

            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            if (!string.IsNullOrEmpty(userBeingUpdatedModel.Password) &&
                !string.IsNullOrEmpty(userBeingUpdatedModel.PasswordConfirmation))
            {
                if (userBeingUpdatedModel.Password != userBeingUpdatedModel.PasswordConfirmation)
                {
                    return new OperationResult<bool>
                    {
                        Errors = new List<Exception> { new Exception("Password does not match confirmation") }
                    };
                }

                var resetToken = await UserManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
                var identityResult =
                    await UserManager.ResetPasswordAsync(user, resetToken, userBeingUpdatedModel.Password).ConfigureAwait(false);
                if (!identityResult.Succeeded)
                {
                    return new OperationResult<bool>
                    {
                        Errors = identityResult.Errors != null
                            ? identityResult.Errors.Select(x =>
                                new Exception($"Code [{x.Code}], Description [{x.Description}]"))
                            : new List<Exception> { new Exception("Unable to reset password") }
                    };
                }
            }

            CacheManager.ClearRegion(Library.Identity.User.CacheRegionUrn(user.RoadieId));

            Logger.LogInformation($"User `{userPerformingUpdate}` modifed user `{userBeingUpdatedModel}`");

            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true
            };
        }
    }
}