using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
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
        private IBookmarkService BookmarkService { get; }

        public PlaylistService(IRoadieSettings configuration,
                    IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            data.IRoadieDbContext dbContext,
            ICacheManager cacheManager,
            ILogger<PlaylistService> logger,
            IBookmarkService bookmarkService)
            : base(configuration, httpEncoder, dbContext, cacheManager, logger, httpContext)
        {
            BookmarkService = bookmarkService;
        }

        public async Task<OperationResult<PlaylistList>> AddNewPlaylist(User user, Playlist model)
        {
            var playlist = new data.Playlist
            {
                IsPublic = model.IsPublic,
                Description = model.Description,
                Name = model.Name,
                UserId = user.Id
            };
            DbContext.Playlists.Add(playlist);
            await DbContext.SaveChangesAsync();
            var r = await AddTracksToPlaylist(playlist,
                model.Tracks.OrderBy(x => x.ListNumber).Select(x => x.Track.Id));
            var request = new PagedRequest
            {
                FilterToPlaylistId = playlist.RoadieId
            };
            var result = await List(request, user);
            return new OperationResult<PlaylistList>
            {
                Data = result.Rows.First(),
                IsSuccess = true
            };
        }

        public async Task<OperationResult<bool>> AddTracksToPlaylist(data.Playlist playlist, IEnumerable<Guid> trackIds)
        {
            var sw = new Stopwatch();
            sw.Start();

            var result = false;
            var now = DateTime.UtcNow;

            var existingTracksForPlaylist = from plt in DbContext.PlaylistTracks
                                            join t in DbContext.Tracks on plt.TrackId equals t.Id
                                            where plt.PlayListId == playlist.Id
                                            select t;
            var newTracksForPlaylist = (from t in DbContext.Tracks
                                        where (from x in trackIds select x).Contains(t.RoadieId)
                                        where !(from x in existingTracksForPlaylist select x.RoadieId).Contains(t.RoadieId)
                                        select t).ToArray();
            foreach (var newTrackForPlaylist in newTracksForPlaylist)
                DbContext.PlaylistTracks.Add(new data.PlaylistTrack
                {
                    TrackId = newTrackForPlaylist.Id,
                    PlayListId = playlist.Id
                });
            playlist.LastUpdated = now;
            await DbContext.SaveChangesAsync();
            result = true;

            var r = await ReorderPlaylist(playlist);
            result = result && r.IsSuccess;

            await UpdatePlaylistCounts(playlist.Id, now);

            sw.Stop();

            return new OperationResult<bool>
            {
                IsSuccess = result,
                Data = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<Playlist>> ById(User roadieUser, Guid id, IEnumerable<string> includes = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = string.Format("urn:playlist_by_id_operation:{0}:{1}", id,
                includes == null ? "0" : string.Join("|", includes));
            var result = await CacheManager.GetAsync(cacheKey, async () => 
            {
                return await PlaylistByIdAction(id, includes);
            }, data.Artist.CacheRegionUrn(id));
            sw.Stop();
            if (result?.Data != null && roadieUser != null)
            {
                if (result?.Data?.Tracks != null)
                {
                    var user = GetUser(roadieUser.UserId);
                    foreach (var track in result.Data.Tracks)
                    {
                        track.Track.TrackPlayUrl = MakeTrackPlayUrl(user, HttpContext.BaseUrl, track.Track.DatabaseId, track.Track.Id);
                    }
                }

                result.Data.UserCanEdit = result.Data.Maintainer.Id == roadieUser.UserId || roadieUser.IsAdmin;
                var userBookmarkResult = await BookmarkService.List(roadieUser, new PagedRequest(), false, BookmarkType.Playlist);
                if (userBookmarkResult.IsSuccess)
                {
                    result.Data.UserBookmarked = userBookmarkResult?.Rows?.FirstOrDefault(x => x.Bookmark.Text == result.Data.Id.ToString()) != null;
                }
                if (result.Data.Comments.Any())
                {
                    var commentIds = result.Data.Comments.Select(x => x.DatabaseId).ToArray();
                    var userCommentReactions = (from cr in DbContext.CommentReactions
                                                where commentIds.Contains(cr.CommentId)
                                                where cr.UserId == roadieUser.Id
                                                select cr).ToArray();
                    foreach (var comment in result.Data.Comments)
                    {
                        var userCommentReaction = userCommentReactions.FirstOrDefault(x => x.CommentId == comment.DatabaseId);
                        comment.IsDisliked = userCommentReaction?.ReactionValue == CommentReaction.Dislike;
                        comment.IsLiked = userCommentReaction?.ReactionValue == CommentReaction.Like;
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

        public async Task<OperationResult<bool>> DeletePlaylist(User user, Guid id)
        {
            var sw = new Stopwatch();
            sw.Start();
            var playlist = DbContext.Playlists.FirstOrDefault(x => x.RoadieId == id);
            if (playlist == null)
            {
                return new OperationResult<bool>(true, string.Format("Playlist Not Found [{0}]", id));
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
            await DbContext.SaveChangesAsync();

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

        public Task<Library.Models.Pagination.PagedResult<PlaylistList>> List(PagedRequest request, User roadieUser = null)
        {
            var sw = new Stopwatch();
            sw.Start();

            var playlistWithArtistTrackIds = new int[0];
            if (request.FilterToArtistId.HasValue)
            {
                playlistWithArtistTrackIds = (from pl in DbContext.Playlists
                                              join pltr in DbContext.PlaylistTracks on pl.Id equals pltr.PlayListId
                                              join t in DbContext.Tracks on pltr.TrackId equals t.Id
                                              join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                              join r in DbContext.Releases on rm.ReleaseId equals r.Id
                                              join a in DbContext.Artists on r.ArtistId equals a.Id
                                              where a.RoadieId == request.FilterToArtistId
                                              select pl.Id
                                             ).ToArray();
            }
            var playlistReleaseTrackIds = new int[0];
            if (request.FilterToReleaseId.HasValue)
            {
                playlistReleaseTrackIds = (from pl in DbContext.Playlists
                                           join pltr in DbContext.PlaylistTracks on pl.Id equals pltr.PlayListId
                                           join t in DbContext.Tracks on pltr.TrackId equals t.Id
                                           join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                           join r in DbContext.Releases on rm.ReleaseId equals r.Id
                                           where r.RoadieId == request.FilterToReleaseId
                                           select pl.Id
                                           ).ToArray();
            }
            var result = from pl in DbContext.Playlists
                         join u in DbContext.Users on pl.UserId equals u.Id
                         where request.FilterToPlaylistId == null || pl.RoadieId == request.FilterToPlaylistId
                         where request.FilterToArtistId == null || playlistWithArtistTrackIds.Contains(pl.Id)
                         where request.FilterToReleaseId == null || playlistReleaseTrackIds.Contains(pl.Id)
                         where roadieUser == null && pl.IsPublic || roadieUser != null && u.RoadieId == roadieUser.UserId || pl.IsPublic
                         where request.FilterValue.Length == 0 || request.FilterValue.Length > 0 && pl.Name != null && pl.Name.Contains(request.FilterValue)
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
                             UserThumbnail = MakeUserThumbnailImage(u.RoadieId),
                             Id = pl.RoadieId,
                             Thumbnail = MakePlaylistThumbnailImage(pl.RoadieId)
                         };
            var sortBy = string.IsNullOrEmpty(request.Sort)
                ? request.OrderValue(new Dictionary<string, string> { { "Playlist.Text", "ASC" } })
                : request.OrderValue();
            var rowCount = result.Count();
            var rows = result
                       .OrderBy(sortBy)
                       .Skip(request.SkipValue)
                       .Take(request.LimitValue)
                       .ToArray();
            sw.Stop();
            return Task.FromResult(new Library.Models.Pagination.PagedResult<PlaylistList>
            {
                TotalCount = rowCount,
                CurrentPage = request.PageValue,
                TotalPages = (int)Math.Ceiling((double)rowCount / request.LimitValue),
                OperationTime = sw.ElapsedMilliseconds,
                Rows = rows
            });
        }

        public async Task<OperationResult<bool>> ReorderPlaylist(data.Playlist playlist)
        {
            var sw = new Stopwatch();
            sw.Start();

            var result = false;
            var now = DateTime.UtcNow;

            if (playlist != null)
            {
                var looper = 0;
                foreach (var playlistTrack in DbContext.PlaylistTracks.Where(x => x.PlayListId == playlist.Id)
                    .OrderBy(x => x.CreatedDate))
                {
                    looper++;
                    playlistTrack.ListNumber = looper;
                    playlistTrack.LastUpdated = now;
                }

                await DbContext.SaveChangesAsync();
                result = true;
            }

            return new OperationResult<bool>
            {
                IsSuccess = result,
                Data = result
            };
        }

        public async Task<OperationResult<bool>> UpdatePlaylist(User user, Playlist model)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var playlist = DbContext.Playlists.FirstOrDefault(x => x.RoadieId == model.Id);
            if (playlist == null)
            {
                return new OperationResult<bool>(true, string.Format("Playlist Not Found [{0}]", model.Id));
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
                    File.WriteAllBytes(playlist.PathToImage(Configuration), ImageHelper.ConvertToJpegFormat(playlistImage));
                    // Update thumbnail
                    playlist.Thumbnail = ImageHelper.ResizeToThumbnail(playlistImage, Configuration);
                }
                playlist.LastUpdated = now;
                await DbContext.SaveChangesAsync();

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
                IsSuccess = !errors.Any(),
                Data = !errors.Any(),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> UpdatePlaylistTracks(User user, PlaylistTrackModifyRequest request)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var playlist = DbContext.Playlists.Include(x => x.Tracks).FirstOrDefault(x => x.RoadieId == request.Id);
            if (playlist == null)
            {
                return new OperationResult<bool>(true, string.Format("Label Not Found [{0}]", request.Id));
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

                var tracks = (from t in DbContext.Tracks
                              join plt in request.Tracks on t.RoadieId equals plt.Track.Id
                              select t).ToArray();
                foreach (var newPlaylistTrack in request.Tracks.OrderBy(x => x.ListNumber))
                {
                    var track = tracks.FirstOrDefault(x => x.RoadieId == newPlaylistTrack.Track.Id);
                    playlist.Tracks.Add(new data.PlaylistTrack
                    {
                        ListNumber = newPlaylistTrack.ListNumber,
                        PlayListId = playlist.Id,
                        CreatedDate = now,
                        TrackId = track.Id
                    });
                }

                playlist.LastUpdated = now;
                await DbContext.SaveChangesAsync();

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
                IsSuccess = !errors.Any(),
                Data = !errors.Any(),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        private Task<OperationResult<Playlist>> PlaylistByIdAction(Guid id, IEnumerable<string> includes = null)
        {
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            var sw = Stopwatch.StartNew();
            sw.Start();

            tsw.Restart();
            var playlist = GetPlaylist(id);
            tsw.Stop();
            timings.Add("getPlaylist", tsw.ElapsedMilliseconds);

            if (playlist == null)
            {
                return Task.FromResult(new OperationResult<Playlist>(true, string.Format("Playlist Not Found [{0}]", id)));
            }
            tsw.Restart();
            var result = playlist.Adapt<Playlist>();
            result.AlternateNames = playlist.AlternateNames;
            result.Tags = playlist.Tags;
            result.URLs = playlist.URLs;
            var maintainer = DbContext.Users.Include(x => x.UserRoles).Include("UserRoles.Role").FirstOrDefault(x => x.Id == playlist.UserId);
            result.Maintainer = UserList.FromDataUser(maintainer, MakeUserThumbnailImage(maintainer.RoadieId));
            result.Thumbnail = MakePlaylistThumbnailImage(playlist.RoadieId);
            result.MediumThumbnail = MakeThumbnailImage(id, "playlist", Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height);
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
                        TrackSize = result.DurationTime,
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
                                             MakeTrackThumbnailImage(plt.t.RoadieId),
                                             MakeReleaseThumbnailImage(r.RoadieId),
                                             MakeArtistThumbnailImage(releaseArtist.RoadieId),
                                             MakeArtistThumbnailImage(trackArtist == null ? null : (Guid?)trackArtist.RoadieId))
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
                            comment.User = UserList.FromDataUser(playlistComment.User, MakeUserThumbnailImage(playlistComment.User.RoadieId));
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
            return Task.FromResult(new OperationResult<Playlist>
            {
                Data = result,
                IsSuccess = result != null,
                OperationTime = sw.ElapsedMilliseconds
            });
        }
    }
}