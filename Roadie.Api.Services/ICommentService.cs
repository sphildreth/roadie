using Roadie.Library;
using Roadie.Library.Enums;
using Roadie.Library.Models.Users;
using System;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ICommentService
    {
        Task<OperationResult<bool>> AddNewArtistCommentAsync(User user, Guid artistId, string cmt);

        Task<OperationResult<bool>> AddNewCollectionCommentAsync(User user, Guid collectionId, string cmt);

        Task<OperationResult<bool>> AddNewGenreCommentAsync(User user, Guid genreId, string cmt);

        Task<OperationResult<bool>> AddNewLabelCommentAsync(User user, Guid labelId, string cmt);

        Task<OperationResult<bool>> AddNewPlaylistCommentAsync(User user, Guid playlistId, string cmt);

        Task<OperationResult<bool>> AddNewReleaseCommentAsync(User user, Guid releaseId, string cmt);

        Task<OperationResult<bool>> AddNewTrackCommentAsync(User user, Guid trackId, string cmt);

        Task<OperationResult<bool>> DeleteCommentAsync(User user, Guid id);

        Task<OperationResult<bool>> SetCommentReactionAsync(User user, Guid id, CommentReaction reaction);
    }
}