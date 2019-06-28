using Roadie.Library;
using Roadie.Library.Enums;
using Roadie.Library.Models.Users;
using System;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ICommentService
    {
        Task<OperationResult<bool>> AddNewArtistComment(User user, Guid artistId, string cmt);

        Task<OperationResult<bool>> AddNewCollectionComment(User user, Guid collectionId, string cmt);

        Task<OperationResult<bool>> AddNewGenreComment(User user, Guid genreId, string cmt);

        Task<OperationResult<bool>> AddNewLabelComment(User user, Guid labelId, string cmt);

        Task<OperationResult<bool>> AddNewPlaylistComment(User user, Guid playlistId, string cmt);

        Task<OperationResult<bool>> AddNewReleaseComment(User user, Guid releaseId, string cmt);

        Task<OperationResult<bool>> AddNewTrackComment(User user, Guid trackId, string cmt);

        Task<OperationResult<bool>> DeleteComment(User user, Guid id);

        Task<OperationResult<bool>> SetCommentReaction(User user, Guid id, CommentReaction reaction);
    }
}