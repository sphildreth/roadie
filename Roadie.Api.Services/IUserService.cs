using Roadie.Library;
using Roadie.Library.Identity;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using System;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IUserService
    {
        Task<OperationResult<User>> ById(User user, Guid id);

        Task<OperationResult<bool>> UpdateProfile(User userPerformingUpdate, User userBeingUpdatedModel);

        Task<PagedResult<UserList>> List(PagedRequest request);

        Task<OperationResult<bool>> SetArtistBookmark(Guid artistId, User roadieUser, bool isBookmarked);

        Task<OperationResult<bool>> SetArtistFavorite(Guid artistId, User roadieUser, bool isFavorite);
        Task<OperationResult<bool>> SetArtistDisliked(Guid artistId, User roadieUser, bool isDisliked);

        Task<OperationResult<short>> SetArtistRating(Guid artistId, User roadieUser, short rating);

        Task<OperationResult<bool>> SetCollectionBookmark(Guid collectionId, User roadieUser, bool isBookmarked);

        Task<OperationResult<bool>> SetLabelBookmark(Guid labelId, User roadieUser, bool isBookmarked);

        Task<OperationResult<bool>> SetPlaylistBookmark(Guid playlistId, User roadieUser, bool isBookmarked);

        Task<OperationResult<bool>> SetReleaseBookmark(Guid releaseid, User roadieUser, bool isBookmarked);

        Task<OperationResult<bool>> SetReleaseFavorite(Guid releaseId, User roadieUser, bool isFavorite);
        Task<OperationResult<bool>> SetReleaseDisliked(Guid releaseId, User roadieUser, bool isDisliked);

        Task<OperationResult<short>> SetReleaseRating(Guid releaseId, User roadieUser, short rating);

        Task<OperationResult<bool>> SetTrackBookmark(Guid trackId, User roadieUser, bool isBookmarked);

        Task<OperationResult<short>> SetTrackRating(Guid trackId, User roadieUser, short rating);
        Task<OperationResult<bool>> SetTrackDisliked(Guid trackId, User roadieUser, bool isDisliked);
        Task<OperationResult<bool>> SetTrackFavorite(Guid releaseId, User roadieUser, bool isFavorite);

    }
}