using Mapster;
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
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Playlists;
using Roadie.Library.Models.Statistics;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public class PlaylistService : ServiceBase, IPlaylistService
    {
        public static Guid DynamicFavoritePlaylistId = Guid.Parse("54BA96A4-DFB5-4970-A989-CBA4BF0EFE75");

        private IBookmarkService BookmarkService { get; }

        public PlaylistService(IRoadieSettings configuration,
                    IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            IRoadieDbContext dbContext,
            ICacheManager cacheManager,
            ILogger<PlaylistService> logger,
            IBookmarkService bookmarkService)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
            BookmarkService = bookmarkService;
        }

        private async Task<OperationResult<Playlist>> PlaylistByIdAction(Guid id, IEnumerable<string> includes = null)
        {
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            var sw = Stopwatch.StartNew();
            sw.Start();

            tsw.Restart();
            var playlist = await GetPlaylist(id).ConfigureAwait(false);
            tsw.Stop();
            timings.Add("getPlaylist", tsw.ElapsedMilliseconds);

            if (playlist == null)
            {
                return new OperationResult<Playlist>(true, $"Playlist Not Found [{id}]");
            }
            tsw.Restart();
            var result = playlist.Adapt<Playlist>();
            result.AlternateNames = playlist.AlternateNames;
            result.Tags = playlist.Tags;
            result.URLs = playlist.URLs;
            var maintainer = DbContext.Users.Include(x => x.UserRoles).Include("UserRoles.Role").FirstOrDefault(x => x.Id == playlist.UserId);
            result.Maintainer = UserList.FromDataUser(maintainer, ImageHelper.MakeUserThumbnailImage(Configuration, HttpContext, maintainer.RoadieId));
            result.Thumbnail = ImageHelper.MakePlaylistThumbnailImage(Configuration, HttpContext, playlist.RoadieId);
            result.MediumThumbnail = ImageHelper.MakeThumbnailImage(Configuration, HttpContext, id, "playlist", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height);
            tsw.Stop();
            timings.Add("adapt", tsw.ElapsedMilliseconds);
            if (includes != null && includes.Any())
            {
                var playlistTracks = (from pl in DbContext.Playlists
                                      join pltr in DbContext.PlaylistTracks on pl.Id equals pltr.PlayListId
                                      join t in DbContext.Tracks on pltr.TrackId equals t.Id
                                      where pl.Id == playlist.Id
                                      select new { t, pltr }).ToArray();

                if (includes.Contains("stats"))
                {
                    tsw.Restart();
                    result.Statistics = new ReleaseGroupingStatistics
                    {
                        ReleaseCount = result.ReleaseCount,
                        TrackCount = result.TrackCount,
                        TrackSize = result.Duration.ToString(),
                        FileSize = playlistTracks.Sum(x => (long?)x.t.FileSize).ToFileSize()
                    };
                    tsw.Stop();
                    timings.Add("stats", tsw.ElapsedMilliseconds);
                }
                if (includes.Contains("tracks"))
                {
                    tsw.Restart();
                    result.Tracks = (from plt in playlistTracks
                                     join rm in DbContext.ReleaseMedias on plt.t.ReleaseMediaId equals rm.Id
                                     join r in DbContext.Releases on rm.ReleaseId equals r.Id
                                     join releaseArtist in DbContext.Artists on r.ArtistId equals releaseArtist.Id
                                     join trackArtist in DbContext.Artists on plt.t.ArtistId equals trackArtist.Id into tas
                                     from trackArtist in tas.DefaultIfEmpty()
                                     select new PlaylistTrack
                                     {
                                         ListNumber = plt.pltr.ListNumber,
                                         Track = TrackList.FromDataTrack(null,
                                             plt.t,
                                             rm.MediaNumber,
                                             r,
                                             releaseArtist,
                                             trackArtist,
                                             HttpContext.BaseUrl,
                                             ImageHelper.MakeTrackThumbnailImage(Configuration, HttpContext, plt.t.RoadieId),
                                             ImageHelper.MakeReleaseThumbnailImage(Configuration, HttpContext, r.RoadieId),
                                             ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, releaseArtist.RoadieId),
                                             ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, trackArtist == null ? null : (Guid?)trackArtist.RoadieId))
                                     }).ToArray();
                    tsw.Stop();
                    timings.Add("tracks", tsw.ElapsedMilliseconds);
                }
                if (includes.Contains("comments"))
                {
                    tsw.Restart();
                    var playlistComments = DbContext.Comments.Include(x => x.User)
                                                    .Where(x => x.PlaylistId == playlist.Id)
                                                    .OrderByDescending(x => x.CreatedDate)
                                                    .ToArray();
                    if (playlistComments.Any())
                    {
                        var comments = new List<Comment>();
                        var commentIds = playlistComments.Select(x => x.Id).ToArray();
                        var userCommentReactions = (from cr in DbContext.CommentReactions
                                                    where commentIds.Contains(cr.CommentId)
                                                    select cr).ToArray();
                        foreach (var playlistComment in playlistComments)
                        {
                            var comment = playlistComment.Adapt<Comment>();
                            comment.DatabaseId = playlistComment.Id;
                            comment.User = UserList.FromDataUser(playlistComment.User, ImageHelper.MakeUserThumbnailImage(Configuration, HttpContext, playlistComment.User.RoadieId));
                            comment.DislikedCount = userCommentReactions.Count(x => x.CommentId == playlistComment.Id && x.ReactionValue == CommentReaction.Dislike);
                            comment.LikedCount = userCommentReactions.Count(x => x.CommentId == playlistComment.Id && x.ReactionValue == CommentReaction.Like);
                            comments.Add(comment);
                        }
                        result.Comments = comments;
                    }
                    tsw.Stop();
                    timings.Add("comments", tsw.ElapsedMilliseconds);
                }
            }

            sw.Stop();
            Logger.LogInformation($"ByIdAction: Playlist `{ playlist }`: includes [{includes.ToCSV()}], timings: [{ timings.ToTimings() }]");
            return new OperationResult<Playlist>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<PlaylistList>> AddNewPlaylistAsync(User user, Playlist model)
        {
            var playlist = new data.Playlist
            {
                IsPublic = model.IsPublic,
                Description = model.Description,
                Name = model.Name,
                UserId = user.Id
            };
            DbContext.Playlists.Add(playlist);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            var r = await AddTracksToPlaylistAsync(playlist, model.Tracks.OrderBy(x => x.ListNumber).Select(x => x.Track.Id)).ConfigureAwait(false);
            var request = new PagedRequest
            {
                FilterToPlaylistId = playlist.RoadieId
            };
            var result = await ListAsync(request, user).ConfigureAwait(false);
            return new OperationResult<PlaylistList>
            {
                Data = result.Rows.First(),
                IsSuccess = true
            };
        }

        public async Task<OperationResult<bool>> AddTracksToPlaylistAsync(data.Playlist playlist, IEnumerable<Guid> trackIds)
        {
            var sw = new Stopwatch();
            sw.Start();

            var result = false;
            var now = DateTime.UtcNow;

            var existingTracksForPlaylist = from plt in DbContext.PlaylistTracks
                                            join t in DbContext.Tracks on plt.TrackId equals t.Id
                                            where plt.PlayListId == playlist.Id
                                            select t;
            var newTracksForPlaylist = await (from t in DbContext.Tracks
                                              where (from x in trackIds select x).Contains(t.RoadieId)
                                              where !(from x in existingTracksForPlaylist select x.RoadieId).Contains(t.RoadieId)
                                              select t)
                                              .ToArrayAsync()
                                              .ConfigureAwait(false);
            foreach (var newTrackForPlaylist in newTracksForPlaylist)
            {
                DbContext.PlaylistTracks.Add(new data.PlaylistTrack
                {
                    TrackId = newTrackForPlaylist.Id,
                    PlayListId = playlist.Id
                });
            }

            playlist.LastUpdated = now;
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            result = true;

            var r = await ReorderPlaylistAsync(playlist).ConfigureAwait(false);
            result = result && r.IsSuccess;

            await UpdatePlaylistCounts(playlist.Id, now).ConfigureAwait(false);

            sw.Stop();

            return new OperationResult<bool>
            {
                IsSuccess = result,
                Data = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<Playlist>> ByIdAsync(User roadieUser, Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();
            OperationResult<Playlist> result = null;
            var isPlaylistDynamic = id == PlaylistService.DynamicFavoritePlaylistId;
            if (isPlaylistDynamic)
            {
                var now = DateTime.UtcNow;
                var userFavoriteTracks = await (from ut in DbContext.UserTracks
                                                join t in DbContext.Tracks on ut.TrackId equals t.Id
                                                join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                join r in DbContext.Releases on rm.ReleaseId equals r.Id
                                                join releaseArtist in DbContext.Artists on r.ArtistId equals releaseArtist.Id
                                                join trackArtist in DbContext.Artists on t.ArtistId equals trackArtist.Id into tas
                                                from trackArtist in tas.DefaultIfEmpty()
                                                where ut.UserId == roadieUser.Id
                                                where ut.IsFavorite == true
                                                orderby ut.CreatedDate descending
                                                select new
                                                {
                                                    r,
                                                    releaseArtist,
                                                    trackArtist,
                                                    rm,
                                                    t
                                                }).ToArrayAsync().ConfigureAwait(false);

                var tracksSize = userFavoriteTracks.Sum(x => x.t.Duration);
                var releaseCount = SafeParser.ToNumber<int?>(userFavoriteTracks.Select(x => x.rm.ReleaseId).Distinct().Count());
                var trackCount = SafeParser.ToNumber<int?>(userFavoriteTracks.Select(x => x.t.Id).Count());
                long? trackFileSize = userFavoriteTracks.Sum(x => (long?)x.t.FileSize);

                var dynamicStatus = new ReleaseGroupingStatistics
                {
                    ReleaseCount = releaseCount,
                    TrackCount = trackCount,
                    TrackSize = tracksSize.ToString(),
                    FileSize = trackFileSize.ToFileSize()
                };
                var dynamicTracks = new ConcurrentBag<PlaylistTrack>();
                Parallel.ForEach(userFavoriteTracks, (td, s, i) =>
                {
                    dynamicTracks.Add(new PlaylistTrack
                    {
                        ListNumber = (int)i,
                        Track = TrackList.FromDataTrack(null,
                                             td.t,
                                             td.rm.MediaNumber,
                                             td.r,
                                             td.releaseArtist,
                                             td.trackArtist,
                                             HttpContext.BaseUrl,
                                             ImageHelper.MakeTrackThumbnailImage(Configuration, HttpContext, td.t.RoadieId),
                                             ImageHelper.MakeReleaseThumbnailImage(Configuration, HttpContext, td.r.RoadieId),
                                             ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, td.releaseArtist.RoadieId),
                                             ImageHelper.MakeArtistThumbnailImage(Configuration, HttpContext, td.trackArtist == null ? null : (Guid?)td.trackArtist.RoadieId))
                    });
                });
                result = new OperationResult<Playlist>
                {
                    Data = new Playlist
                    {
                        Id = id,
                        Name = "[r] Favorites",
                        Description = "Dynamic Playlist of Favorited Tracks",
                        CreatedDate = now,
                        LastUpdated = now,
                        UserCanEdit = false,
                        IsPublic = false,
                        Maintainer = new UserList
                        {
                            IsAdmin = false,
                            IsEditor = false,
                            IsPrivate = true,
                            User = new DataToken
                            {
                                Text = "Roadie System"
                            },
                            Thumbnail = ImageHelper.MakeUserThumbnailImage(Configuration, HttpContext, roadieUser.UserId)
                        },
                        Statistics = dynamicStatus,
                        ReleaseCount = SafeParser.ToNumber<short?>(dynamicStatus.ReleaseCount),
                        TrackCount = SafeParser.ToNumber<short?>(dynamicStatus.TrackCount),
                        Duration = userFavoriteTracks.Sum(x => x.t.Duration),
                        Thumbnail = ImageHelper.MakePlaylistThumbnailImage(Configuration, HttpContext, id),
                        Tracks = dynamicTracks.OrderBy(x => x.ListNumber),
                        MediumThumbnail = ImageHelper.MakeThumbnailImage(Configuration, HttpContext, id, "playlist", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height)
                    },
                    IsSuccess = true
                };
            }
            else
            {
                var cacheKey = $"urn:playlist_by_id_operation:{id}:{(includes == null ? "0" : string.Join("|", includes))}";
                result = await CacheManager.GetAsync(cacheKey, async () =>
                {
                    return await PlaylistByIdAction(id, includes).ConfigureAwait(false);
                }, data.Artist.CacheRegionUrn(id)).ConfigureAwait(false);
            }
            sw.Stop();
            if (result?.Data != null && roadieUser != null)
            {
                if (result?.Data?.Tracks != null)
                {
                    var user = await GetUser(roadieUser.UserId).ConfigureAwait(false);
                    foreach (var track in result.Data.Tracks)
                    {
                        track.Track.TrackPlayUrl = MakeTrackPlayUrl(user, HttpContext.BaseUrl, track.Track.Id);
                    }
                }
                if (!isPlaylistDynamic)
                {
                    result.Data.UserCanEdit = result.Data.Maintainer?.Id == roadieUser.UserId || roadieUser.IsAdmin;
                    var userBookmarkResult = await BookmarkService.ListAsync(roadieUser, new PagedRequest(), false, BookmarkType.Playlist).ConfigureAwait(false);
                    if (userBookmarkResult.IsSuccess)
                    {
                        result.Data.UserBookmarked = userBookmarkResult?.Rows?.FirstOrDefault(x => x?.Bookmark?.Value == result?.Data?.Id?.ToString()) != null;
                    }
                    if (result.Data.Comments?.Any() == true)
                    {
                        var commentIds = result.Data.Comments.Select(x => x.DatabaseId).ToArray();
                        var userCommentReactions = await (from cr in DbContext.CommentReactions
                                                          where commentIds.Contains(cr.CommentId)
                                                          where cr.UserId == roadieUser.Id
                                                          select cr)
                                                    .ToArrayAsync()
                                                    .ConfigureAwait(false);
                        foreach (var comment in result.Data.Comments)
                        {
                            var userCommentReaction = Array.Find(userCommentReactions, x => x.CommentId == comment.DatabaseId);
                            comment.IsDisliked = userCommentReaction?.ReactionValue == CommentReaction.Dislike;
                            comment.IsLiked = userCommentReaction?.ReactionValue == CommentReaction.Like;
                        }
                    }
                }
            }

            return new OperationResult<Playlist>(result.Messages)
            {
                Data = result?.Data,
                IsNotFoundResult = result?.IsNotFoundResult ?? false,
                Errors = result?.Errors,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> DeletePlaylistAsync(User user, Guid id)
        {
            var sw = new Stopwatch();
            sw.Start();
            var playlist = await DbContext.Playlists.FirstOrDefaultAsync(x => x.RoadieId == id).ConfigureAwait(false);
            if (playlist == null)
            {
                return new OperationResult<bool>(true, $"Playlist Not Found [{id}]");
            }
            if (!user.IsAdmin && user.Id != playlist.UserId)
            {
                Logger.LogWarning("User `{0}` attempted to delete Playlist `{1}`", user, playlist);
                return new OperationResult<bool>("Access Denied")
                {
                    IsAccessDeniedResult = true
                };
            }

            DbContext.Playlists.Remove(playlist);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            await BookmarkService.RemoveAllBookmarksForItemAsync(BookmarkType.Playlist, playlist.Id).ConfigureAwait(false);

            var playlistImageFilename = playlist.PathToImage(Configuration);
            if (File.Exists(playlistImageFilename))
            {
                File.Delete(playlistImageFilename);
            }

            Logger.LogWarning("User `{0}` deleted Playlist `{1}]`", user, playlist);
            CacheManager.ClearRegion(playlist.CacheRegion);
            sw.Stop();
            return new OperationResult<bool>
            {
                IsSuccess = true,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<Library.Models.Pagination.PagedResult<PlaylistList>> ListAsync(PagedRequest request, User roadieUser = null)
        {
            var sw = new Stopwatch();
            sw.Start();

            var playlistWithArtistTrackIds = new int[0];
            if (request.FilterToArtistId.HasValue)
            {
                playlistWithArtistTrackIds = await (from pl in DbContext.Playlists
                                                    join pltr in DbContext.PlaylistTracks on pl.Id equals pltr.PlayListId
                                                    join t in DbContext.Tracks on pltr.TrackId equals t.Id
                                                    join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                    join r in DbContext.Releases on rm.ReleaseId equals r.Id
                                                    join a in DbContext.Artists on r.ArtistId equals a.Id
                                                    where a.RoadieId == request.FilterToArtistId
                                                    select pl.Id
                                             ).ToArrayAsync().ConfigureAwait(false);
            }
            var playlistReleaseTrackIds = new int[0];
            if (request.FilterToReleaseId.HasValue)
            {
                playlistReleaseTrackIds = await (from pl in DbContext.Playlists
                                                 join pltr in DbContext.PlaylistTracks on pl.Id equals pltr.PlayListId
                                                 join t in DbContext.Tracks on pltr.TrackId equals t.Id
                                                 join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                                 join r in DbContext.Releases on rm.ReleaseId equals r.Id
                                                 where r.RoadieId == request.FilterToReleaseId
                                                 select pl.Id
                                           ).ToArrayAsync().ConfigureAwait(false);
            }
            var normalizedFilterValue = !string.IsNullOrEmpty(request.FilterValue)
                ? request.FilterValue.ToAlphanumericName()
                : null;
            var result = from pl in DbContext.Playlists
                         join u in DbContext.Users on pl.UserId equals u.Id
                         where request.FilterToPlaylistId == null || pl.RoadieId == request.FilterToPlaylistId
                         where request.FilterToArtistId == null || playlistWithArtistTrackIds.Contains(pl.Id)
                         where request.FilterToReleaseId == null || playlistReleaseTrackIds.Contains(pl.Id)
                         where roadieUser == null && pl.IsPublic || roadieUser != null && u.RoadieId == roadieUser.UserId || pl.IsPublic
                         where string.IsNullOrEmpty(normalizedFilterValue) || (
                                   pl.Name.ToLower().Contains(normalizedFilterValue) ||
                                   pl.AlternateNames.ToLower().Contains(normalizedFilterValue)
                               )
                         select new PlaylistList
                         {
                             Playlist = new DataToken
                             {
                                 Text = pl.Name,
                                 Value = pl.RoadieId.ToString()
                             },
                             User = new DataToken
                             {
                                 Text = u.UserName,
                                 Value = u.RoadieId.ToString()
                             },
                             PlaylistCount = pl.TrackCount,
                             IsPublic = pl.IsPublic,
                             Duration = pl.Duration,
                             TrackCount = pl.TrackCount,
                             CreatedDate = pl.CreatedDate,
                             LastUpdated = pl.LastUpdated,
                             UserThumbnail = ImageHelper.MakeUserThumbnailImage(Configuration, HttpContext, u.RoadieId),
                             Id = pl.RoadieId,
                             Thumbnail = ImageHelper.MakePlaylistThumbnailImage(Configuration, HttpContext, pl.RoadieId)
                         };
            var sortBy = string.IsNullOrEmpty(request.Sort)
                ? request.OrderValue(new Dictionary<string, string> { { "Playlist.Text", "ASC" } })
                : request.OrderValue();
            var rowCount = await result.CountAsync().ConfigureAwait(false);
            var rows = await result
                       .OrderBy(sortBy)
                       .Skip(request.SkipValue)
                       .Take(request.LimitValue)
                       .ToArrayAsync()
                       .ConfigureAwait(false);

            // Dynamic list of favorites
            if (string.IsNullOrWhiteSpace(normalizedFilterValue) && 
                !request.FilterToArtistId.HasValue && !request.FilterToReleaseId.HasValue && roadieUser != null)
            {
                var userFavoriteTracks = from ut in DbContext.UserTracks
                                         join t in DbContext.Tracks on ut.TrackId equals t.Id
                                         where ut.UserId == roadieUser.Id
                                         where ut.IsFavorite == true
                                         select t;

                var numberOfFavorites = SafeParser.ToNumber<short?>(await userFavoriteTracks.CountAsync());

                if (numberOfFavorites > 0)
                {
                    var now = DateTime.UtcNow;
                    var dynamicPlaylist = new PlaylistList
                    {
                        Playlist = new DataToken
                        {
                            Text = "[r] Favorites",
                            Value = DynamicFavoritePlaylistId.ToString()
                        },
                        User = new DataToken
                        {
                            Text = roadieUser.UserName,
                            Value = roadieUser.UserId.ToString()
                        },
                        PlaylistCount = numberOfFavorites.Value,
                        IsPublic = false,
                        Duration = userFavoriteTracks.Sum(x => x.Duration),
                        TrackCount = numberOfFavorites.Value,
                        CreatedDate = now,
                        LastUpdated = now,
                        UserThumbnail = ImageHelper.MakeUserThumbnailImage(Configuration, HttpContext, roadieUser.UserId),
                        Id = DynamicFavoritePlaylistId,
                        Thumbnail = ImageHelper.MakePlaylistThumbnailImage(Configuration, HttpContext, DynamicFavoritePlaylistId)
                    };
                    rows = new List<PlaylistList>(rows.Concat<PlaylistList>(new PlaylistList[1] { dynamicPlaylist })).ToArray();
                }
            }
            sw.Stop();
            return new Library.Models.Pagination.PagedResult<PlaylistList>
            {
                TotalCount = rowCount,
                CurrentPage = request.PageValue,
                TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                OperationTime = sw.ElapsedMilliseconds,
                Rows = rows
            };
        }

        public async Task<OperationResult<bool>> ReorderPlaylistAsync(data.Playlist playlist)
        {
            var sw = new Stopwatch();
            sw.Start();

            var result = false;
            var now = DateTime.UtcNow;

            if (playlist != null)
            {
                var looper = 0;
                foreach (var playlistTrack in DbContext.PlaylistTracks.Where(x => x.PlayListId == playlist.Id).OrderBy(x => x.CreatedDate))
                {
                    looper++;
                    playlistTrack.ListNumber = looper;
                    playlistTrack.LastUpdated = now;
                }

                await DbContext.SaveChangesAsync().ConfigureAwait(false);
                result = true;
            }

            return new OperationResult<bool>
            {
                IsSuccess = result,
                Data = result
            };
        }

        public async Task<OperationResult<bool>> UpdatePlaylistAsync(User user, Playlist model)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var playlist = await DbContext.Playlists.FirstOrDefaultAsync(x => x.RoadieId == model.Id).ConfigureAwait(false);
            if (playlist == null)
            {
                return new OperationResult<bool>(true, $"Playlist Not Found [{model.Id}]");
            }
            if (!user.IsAdmin && user.Id != playlist.UserId)
            {
                Logger.LogWarning("User `{0}` attempted to update Playlist `{1}`", user, playlist);
                return new OperationResult<bool>("Access Denied")
                {
                    IsAccessDeniedResult = true
                };
            }
            try
            {
                var now = DateTime.UtcNow;
                playlist.AlternateNames = model.AlternateNamesList.ToDelimitedList();
                playlist.Description = model.Description;
                playlist.IsLocked = model.IsLocked;
                playlist.IsPublic = model.IsPublic;
                var oldPathToImage = playlist.PathToImage(Configuration);
                var didChangeName = playlist.Name != model.Name;
                playlist.Name = model.Name;
                playlist.Status = SafeParser.ToEnum<Statuses>(model.Status);
                playlist.Tags = model.TagsList.ToDelimitedList();
                playlist.URLs = model.URLsList.ToDelimitedList();

                if (didChangeName)
                {
                    if (File.Exists(oldPathToImage))
                    {
                        File.Move(oldPathToImage, playlist.PathToImage(Configuration));
                    }
                }

                var playlistImage = ImageHelper.ImageDataFromUrl(model.NewThumbnailData);
                if (playlistImage != null)
                {
                    // Save unaltered playlist image
                    await File.WriteAllBytesAsync(playlist.PathToImage(Configuration, true), ImageHelper.ConvertToJpegFormat(playlistImage)).ConfigureAwait(false);
                }
                playlist.LastUpdated = now;
                await DbContext.SaveChangesAsync().ConfigureAwait(false);

                CacheManager.ClearRegion(playlist.CacheRegion);
                Logger.LogInformation($"UpdatePlaylist `{playlist}` By User `{user}`");
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

        public async Task<OperationResult<bool>> UpdatePlaylistTracksAsync(User user, PlaylistTrackModifyRequest request)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var playlist = await DbContext.Playlists.Include(x => x.Tracks).FirstOrDefaultAsync(x => x.RoadieId == request.Id).ConfigureAwait(false);
            if (playlist == null)
            {
                return new OperationResult<bool>(true, $"Label Not Found [{request.Id}]");
            }
            if (!user.IsAdmin && user.Id != playlist.UserId)
            {
                Logger.LogWarning("User `{0}` attempted to update Playlist Tracks `{1}`", user, playlist);
                return new OperationResult<bool>("Access Denied")
                {
                    IsAccessDeniedResult = true
                };
            }
            try
            {
                var now = DateTime.UtcNow;

                playlist.Tracks.Clear();

                var tracks = await (from t in DbContext.Tracks
                                    join plt in request.Tracks on t.RoadieId equals plt.Track.Id
                                    select t).ToArrayAsync().ConfigureAwait(false);
                foreach (var newPlaylistTrack in request.Tracks.OrderBy(x => x.ListNumber))
                {
                    var track = Array.Find(tracks, x => x.RoadieId == newPlaylistTrack.Track.Id);
                    playlist.Tracks.Add(new data.PlaylistTrack
                    {
                        ListNumber = newPlaylistTrack.ListNumber,
                        PlayListId = playlist.Id,
                        CreatedDate = now,
                        TrackId = track.Id
                    });
                }

                playlist.LastUpdated = now;
                await DbContext.SaveChangesAsync().ConfigureAwait(false);

                //  await base.UpdatePlaylistCounts(playlist.Id, now);

                Logger.LogInformation($"UpdatePlaylistTracks `{playlist}` By User `{user}`");
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