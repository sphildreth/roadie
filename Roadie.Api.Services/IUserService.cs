using Roadie.Library;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IUserService
    {
        Task<OperationResult<User>> ByIdAsync(User user, Guid id, IEnumerable<string> includes, bool isAccountSettingsEdit = false);

        Task<OperationResult<bool>> DeleteAllBookmarksAsync(User roadieUser);

        Task<PagedResult<UserList>> ListAsync(PagedRequest request);

        Task<OperationResult<bool>> SetArtistBookmarkAsync(Guid artistId, User roadieUser, bool isBookmarked);

        Task<OperationResult<bool>> SetArtistDislikedAsync(Guid artistId, User roadieUser, bool isDisliked);

        Task<OperationResult<bool>> SetArtistFavoriteAsync(Guid artistId, User roadieUser, bool isFavorite);

        Task<OperationResult<short>> SetArtistRatingAsync(Guid artistId, User roadieUser, short rating);

        Task<OperationResult<bool>> SetCollectionBookmarkAsync(Guid collectionId, User roadieUser, bool isBookmarked);

        Task<OperationResult<bool>> SetLabelBookmarkAsync(Guid labelId, User roadieUser, bool isBookmarked);

        Task<OperationResult<bool>> SetPlaylistBookmarkAsync(Guid playlistId, User roadieUser, bool isBookmarked);

        Task<OperationResult<bool>> SetReleaseBookmarkAsync(Guid releaseid, User roadieUser, bool isBookmarked);

        Task<OperationResult<bool>> SetReleaseDislikedAsync(Guid releaseId, User roadieUser, bool isDisliked);

        Task<OperationResult<bool>> SetReleaseFavoriteAsync(Guid releaseId, User roadieUser, bool isFavorite);

        Task<OperationResult<short>> SetReleaseRatingAsync(Guid releaseId, User roadieUser, short rating);

        Task<OperationResult<bool>> SetTrackBookmarkAsync(Guid trackId, User roadieUser, bool isBookmarked);

        Task<OperationResult<bool>> SetTrackDislikedAsync(Guid trackId, User roadieUser, bool isDisliked);

        Task<OperationResult<bool>> SetTrackFavoriteAsync(Guid releaseId, User roadieUser, bool isFavorite);

        Task<OperationResult<short>> SetTrackRatingAsync(Guid trackId, User roadieUser, short rating);

        Task<OperationResult<bool>> UpdateIntegrationGrantAsync(Guid userId, string integrationName, string token);

        Task<OperationResult<bool>> UpdateProfileAsync(User userPerformingUpdate, User userBeingUpdatedModel);
    }
}