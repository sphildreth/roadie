using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public class CommentService : ServiceBase, ICommentService
    {
        public CommentService(IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            IRoadieDbContext context,
            ICacheManager cacheManager,
            ILogger<CommentService> logger)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
        }

        private void ClearCaches(data.Comment comment)
        {
            switch (comment.CommentType)
            {
                case CommentType.Artist:
                    var artist = DbContext.Artists.FirstOrDefault(x => x.Id == comment.ArtistId);
                    if (artist != null)
                    {
                        CacheManager.ClearRegion(artist.CacheRegion);
                    }

                    break;

                case CommentType.Collection:
                    var collection = DbContext.Collections.FirstOrDefault(x => x.Id == comment.CollectionId);
                    if (collection != null)
                    {
                        CacheManager.ClearRegion(collection.CacheRegion);
                    }

                    break;

                case CommentType.Genre:
                    var genre = DbContext.Genres.FirstOrDefault(x => x.Id == comment.GenreId);
                    if (genre != null)
                    {
                        CacheManager.ClearRegion(genre.CacheRegion);
                    }

                    break;

                case CommentType.Label:
                    var label = DbContext.Labels.FirstOrDefault(x => x.Id == comment.LabelId);
                    if (label != null)
                    {
                        CacheManager.ClearRegion(label.CacheRegion);
                    }

                    break;

                case CommentType.Playlist:
                    var playlist = DbContext.Playlists.FirstOrDefault(x => x.Id == comment.PlaylistId);
                    if (playlist != null)
                    {
                        CacheManager.ClearRegion(playlist.CacheRegion);
                    }

                    break;

                case CommentType.Release:
                    var release = DbContext.Releases.FirstOrDefault(x => x.Id == comment.ReleaseId);
                    if (release != null)
                    {
                        CacheManager.ClearRegion(release.CacheRegion);
                    }

                    break;

                case CommentType.Track:
                    var track = DbContext.Tracks.FirstOrDefault(x => x.Id == comment.TrackId);
                    if (track != null)
                    {
                        CacheManager.ClearRegion(track.CacheRegion);
                    }

                    break;
            }
        }

        public async Task<OperationResult<bool>> AddNewArtistCommentAsync(User user, Guid artistId, string cmt)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var artist = await DbContext.Artists.FirstOrDefaultAsync(x => x.RoadieId == artistId).ConfigureAwait(false);
            if (artist == null)
            {
                return new OperationResult<bool>(true, $"Artist Not Found [{artistId}]");
            }

            var newComment = new data.Comment
            {
                ArtistId = artist.Id,
                UserId = user.Id.Value,
                Cmt = cmt
            };
            await DbContext.Comments.AddAsync(newComment).ConfigureAwait(false);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            CacheManager.ClearRegion(artist.CacheRegion);
            sw.Stop();
            result = true;
            return new OperationResult<bool>
            {
                Data = result,
                IsSuccess = result,
                Errors = errors,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> AddNewCollectionCommentAsync(User user, Guid collectionId, string cmt)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var collection = await DbContext.Collections.FirstOrDefaultAsync(x => x.RoadieId == collectionId).ConfigureAwait(false);
            if (collection == null)
            {
                return new OperationResult<bool>(true, $"Collection Not Found [{collectionId}]");
            }

            var newComment = new data.Comment
            {
                CollectionId = collection.Id,
                UserId = user.Id.Value,
                Cmt = cmt
            };
            await DbContext.Comments.AddAsync(newComment).ConfigureAwait(false);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            CacheManager.ClearRegion(collection.CacheRegion);
            sw.Stop();
            result = true;
            return new OperationResult<bool>
            {
                Data = result,
                IsSuccess = result,
                Errors = errors,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> AddNewGenreCommentAsync(User user, Guid genreId, string cmt)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var genre = await DbContext.Genres.FirstOrDefaultAsync(x => x.RoadieId == genreId).ConfigureAwait(false);
            if (genre == null)
            {
                return new OperationResult<bool>(true, $"Genre Not Found [{genreId}]");
            }

            var newComment = new data.Comment
            {
                GenreId = genre.Id,
                UserId = user.Id.Value,
                Cmt = cmt
            };
            await DbContext.Comments.AddAsync(newComment).ConfigureAwait(false);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            CacheManager.ClearRegion(genre.CacheRegion);
            sw.Stop();
            result = true;
            return new OperationResult<bool>
            {
                Data = result,
                IsSuccess = result,
                Errors = errors,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> AddNewLabelCommentAsync(User user, Guid labelId, string cmt)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var label = await DbContext.Labels.FirstOrDefaultAsync(x => x.RoadieId == labelId).ConfigureAwait(false);
            if (label == null)
            {
                return new OperationResult<bool>(true, $"Label Not Found [{labelId}]");
            }

            var newComment = new data.Comment
            {
                LabelId = label.Id,
                UserId = user.Id.Value,
                Cmt = cmt
            };
            await DbContext.Comments.AddAsync(newComment).ConfigureAwait(false);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            CacheManager.ClearRegion(label.CacheRegion);
            sw.Stop();
            result = true;
            return new OperationResult<bool>
            {
                Data = result,
                IsSuccess = result,
                Errors = errors,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> AddNewPlaylistCommentAsync(User user, Guid playlistId, string cmt)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var playlist = await DbContext.Playlists.FirstOrDefaultAsync(x => x.RoadieId == playlistId).ConfigureAwait(false);
            if (playlist == null)
            {
                return new OperationResult<bool>(true, $"Playlist Not Found [{playlistId}]");
            }

            var newComment = new data.Comment
            {
                PlaylistId = playlist.Id,
                UserId = user.Id.Value,
                Cmt = cmt
            };
            await DbContext.Comments.AddAsync(newComment).ConfigureAwait(false);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            CacheManager.ClearRegion(playlist.CacheRegion);
            sw.Stop();
            result = true;
            return new OperationResult<bool>
            {
                Data = result,
                IsSuccess = result,
                Errors = errors,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> AddNewReleaseCommentAsync(User user, Guid releaseId, string cmt)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var release = await DbContext.Releases.FirstOrDefaultAsync(x => x.RoadieId == releaseId).ConfigureAwait(false);
            if (release == null)
            {
                return new OperationResult<bool>(true, $"Release Not Found [{releaseId}]");
            }

            var newComment = new data.Comment
            {
                ReleaseId = release.Id,
                UserId = user.Id.Value,
                Cmt = cmt
            };
            await DbContext.Comments.AddAsync(newComment).ConfigureAwait(false);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            CacheManager.ClearRegion(release.CacheRegion);
            sw.Stop();
            result = true;
            return new OperationResult<bool>
            {
                Data = result,
                IsSuccess = result,
                Errors = errors,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> AddNewTrackCommentAsync(User user, Guid trackId, string cmt)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var track = await DbContext.Tracks.FirstOrDefaultAsync(x => x.RoadieId == trackId).ConfigureAwait(false);
            if (track == null)
            {
                return new OperationResult<bool>(true, $"Track Not Found [{trackId}]");
            }

            var newComment = new data.Comment
            {
                TrackId = track.Id,
                UserId = user.Id.Value,
                Cmt = cmt
            };
            await DbContext.Comments.AddAsync(newComment).ConfigureAwait(false);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            CacheManager.ClearRegion(track.CacheRegion);
            sw.Stop();
            result = true;
            return new OperationResult<bool>
            {
                Data = result,
                IsSuccess = result,
                Errors = errors,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> DeleteCommentAsync(User user, Guid id)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var comment = await DbContext.Comments.FirstOrDefaultAsync(x => x.RoadieId == id).ConfigureAwait(false);
            if (comment == null)
            {
                return new OperationResult<bool>(true, $"Comment Not Found [{id}]");
            }

            DbContext.Remove(comment);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            ClearCaches(comment);
            sw.Stop();
            result = true;
            return new OperationResult<bool>
            {
                Data = result,
                IsSuccess = result,
                Errors = errors,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> SetCommentReactionAsync(User user, Guid id, CommentReaction reaction)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var comment = await DbContext.Comments.FirstOrDefaultAsync(x => x.RoadieId == id).ConfigureAwait(false);
            if (comment == null)
            {
                return new OperationResult<bool>(true, $"Comment Not Found [{id}]");
            }

            var userCommentReaction = await DbContext.CommentReactions.FirstOrDefaultAsync(x => x.CommentId == comment.Id && x.UserId == user.Id).ConfigureAwait(false);
            if (userCommentReaction == null)
            {
                userCommentReaction = new data.CommentReaction
                {
                    CommentId = comment.Id,
                    UserId = user.Id.Value
                };
                await DbContext.CommentReactions.AddAsync(userCommentReaction).ConfigureAwait(false);
            }

            userCommentReaction.Reaction = reaction == CommentReaction.Unknown ? null : reaction.ToString();
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            ClearCaches(comment);
            var userCommentReactions = await (from cr in DbContext.CommentReactions
                                        where cr.CommentId == comment.Id
                                        select cr)
                                        .ToArrayAsync()
                                        .ConfigureAwait(false);
            var additionalData = new Dictionary<string, object>();
            additionalData.Add("likedCount", userCommentReactions.Where(x => x.ReactionValue == CommentReaction.Like).Count());
            additionalData.Add("dislikedCount", userCommentReactions.Where(x => x.ReactionValue == CommentReaction.Dislike).Count());
            sw.Stop();
            result = true;
            return new OperationResult<bool>
            {
                AdditionalClientData = additionalData,
                Data = result,
                IsSuccess = result,
                Errors = errors,
                OperationTime = sw.ElapsedMilliseconds
            };
        }
    }
}