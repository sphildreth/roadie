using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
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
            data.IRoadieDbContext context,
            ICacheManager cacheManager,
            ILogger<CommentService> logger)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
        }

        public async Task<OperationResult<bool>> AddNewArtistComment(User user, Guid artistId, string cmt)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var artist = DbContext.Artists
                .FirstOrDefault(x => x.RoadieId == artistId);
            if (artist == null)
                return new OperationResult<bool>(true, string.Format("Artist Not Found [{0}]", artistId));

            var newComment = new data.Comment
            {
                ArtistId = artist.Id,
                UserId = user.Id.Value,
                Cmt = cmt
            };
            DbContext.Comments.Add(newComment);
            await DbContext.SaveChangesAsync();
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

        public async Task<OperationResult<bool>> AddNewCollectionComment(User user, Guid collectionId, string cmt)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var collection = DbContext.Collections
                .FirstOrDefault(x => x.RoadieId == collectionId);
            if (collection == null)
                return new OperationResult<bool>(true, string.Format("Collection Not Found [{0}]", collectionId));

            var newComment = new data.Comment
            {
                CollectionId = collection.Id,
                UserId = user.Id.Value,
                Cmt = cmt
            };
            DbContext.Comments.Add(newComment);
            await DbContext.SaveChangesAsync();
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

        public async Task<OperationResult<bool>> AddNewGenreComment(User user, Guid genreId, string cmt)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var genre = DbContext.Genres
                .FirstOrDefault(x => x.RoadieId == genreId);
            if (genre == null) return new OperationResult<bool>(true, string.Format("Genre Not Found [{0}]", genreId));

            var newComment = new data.Comment
            {
                GenreId = genre.Id,
                UserId = user.Id.Value,
                Cmt = cmt
            };
            DbContext.Comments.Add(newComment);
            await DbContext.SaveChangesAsync();
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

        public async Task<OperationResult<bool>> AddNewLabelComment(User user, Guid labelId, string cmt)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var label = DbContext.Labels
                .FirstOrDefault(x => x.RoadieId == labelId);
            if (label == null) return new OperationResult<bool>(true, string.Format("Label Not Found [{0}]", labelId));

            var newComment = new data.Comment
            {
                LabelId = label.Id,
                UserId = user.Id.Value,
                Cmt = cmt
            };
            DbContext.Comments.Add(newComment);
            await DbContext.SaveChangesAsync();
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

        public async Task<OperationResult<bool>> AddNewPlaylistComment(User user, Guid playlistId, string cmt)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var playlist = DbContext.Playlists
                .FirstOrDefault(x => x.RoadieId == playlistId);
            if (playlist == null)
                return new OperationResult<bool>(true, string.Format("Playlist Not Found [{0}]", playlistId));

            var newComment = new data.Comment
            {
                PlaylistId = playlist.Id,
                UserId = user.Id.Value,
                Cmt = cmt
            };
            DbContext.Comments.Add(newComment);
            await DbContext.SaveChangesAsync();
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

        public async Task<OperationResult<bool>> AddNewReleaseComment(User user, Guid releaseId, string cmt)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var release = DbContext.Releases
                .FirstOrDefault(x => x.RoadieId == releaseId);
            if (release == null)
                return new OperationResult<bool>(true, string.Format("Release Not Found [{0}]", releaseId));

            var newComment = new data.Comment
            {
                ReleaseId = release.Id,
                UserId = user.Id.Value,
                Cmt = cmt
            };
            DbContext.Comments.Add(newComment);
            await DbContext.SaveChangesAsync();
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

        public async Task<OperationResult<bool>> AddNewTrackComment(User user, Guid trackId, string cmt)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var track = DbContext.Tracks
                .FirstOrDefault(x => x.RoadieId == trackId);
            if (track == null) return new OperationResult<bool>(true, string.Format("Track Not Found [{0}]", trackId));

            var newComment = new data.Comment
            {
                TrackId = track.Id,
                UserId = user.Id.Value,
                Cmt = cmt
            };
            DbContext.Comments.Add(newComment);
            await DbContext.SaveChangesAsync();
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

        public async Task<OperationResult<bool>> DeleteComment(User user, Guid id)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var comment = DbContext.Comments.FirstOrDefault(x => x.RoadieId == id);
            if (comment == null) return new OperationResult<bool>(true, string.Format("Comment Not Found [{0}]", id));
            DbContext.Remove(comment);
            await DbContext.SaveChangesAsync();
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

        public async Task<OperationResult<bool>> SetCommentReaction(User user, Guid id, CommentReaction reaction)
        {
            var sw = Stopwatch.StartNew();
            var result = false;
            var errors = new List<Exception>();
            var comment = DbContext.Comments.FirstOrDefault(x => x.RoadieId == id);
            if (comment == null) return new OperationResult<bool>(true, string.Format("Comment Not Found [{0}]", id));
            var userCommentReaction =
                DbContext.CommentReactions.FirstOrDefault(x => x.CommentId == comment.Id && x.UserId == user.Id);
            if (userCommentReaction == null)
            {
                userCommentReaction = new data.CommentReaction
                {
                    CommentId = comment.Id,
                    UserId = user.Id.Value
                };
                DbContext.CommentReactions.Add(userCommentReaction);
            }

            userCommentReaction.Reaction = reaction == CommentReaction.Unknown ? null : reaction.ToString();
            await DbContext.SaveChangesAsync();
            ClearCaches(comment);
            var userCommentReactions = (from cr in DbContext.CommentReactions
                                        where cr.CommentId == comment.Id
                                        select cr).ToArray();
            var additionalData = new Dictionary<string, object>();
            additionalData.Add("likedCount",
                userCommentReactions.Where(x => x.ReactionValue == CommentReaction.Like).Count());
            additionalData.Add("dislikedCount",
                userCommentReactions.Where(x => x.ReactionValue == CommentReaction.Dislike).Count());
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

        private void ClearCaches(data.Comment comment)
        {
            switch (comment.CommentType)
            {
                case CommentType.Artist:
                    var artist = DbContext.Artists.FirstOrDefault(x => x.Id == comment.ArtistId);
                    if (artist != null) CacheManager.ClearRegion(artist.CacheRegion);
                    break;

                case CommentType.Collection:
                    var collection = DbContext.Collections.FirstOrDefault(x => x.Id == comment.CollectionId);
                    if (collection != null) CacheManager.ClearRegion(collection.CacheRegion);
                    break;

                case CommentType.Genre:
                    var genre = DbContext.Genres.FirstOrDefault(x => x.Id == comment.GenreId);
                    if (genre != null) CacheManager.ClearRegion(genre.CacheRegion);
                    break;

                case CommentType.Label:
                    var label = DbContext.Labels.FirstOrDefault(x => x.Id == comment.LabelId);
                    if (label != null) CacheManager.ClearRegion(label.CacheRegion);
                    break;

                case CommentType.Playlist:
                    var playlist = DbContext.Playlists.FirstOrDefault(x => x.Id == comment.PlaylistId);
                    if (playlist != null) CacheManager.ClearRegion(playlist.CacheRegion);
                    break;

                case CommentType.Release:
                    var release = DbContext.Releases.FirstOrDefault(x => x.Id == comment.ReleaseId);
                    if (release != null) CacheManager.ClearRegion(release.CacheRegion);
                    break;

                case CommentType.Track:
                    var track = DbContext.Tracks.FirstOrDefault(x => x.Id == comment.TrackId);
                    if (track != null) CacheManager.ClearRegion(track.CacheRegion);
                    break;
            }
        }
    }
}