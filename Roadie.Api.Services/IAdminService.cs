using Microsoft.AspNetCore.Identity;
using Roadie.Library;
using Roadie.Library.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IAdminService
    {
        Task<OperationResult<bool>> DeleteArtistAsync(User user, Guid artistId, bool deleteFolder);

        Task<OperationResult<bool>> DeleteArtistReleasesAsync(User user, Guid artistId, bool doDeleteFiles = false);

        Task<OperationResult<bool>> DeleteArtistSecondaryImageAsync(User user, Guid artistId, int index);

        Task<OperationResult<bool>> DeleteGenreAsync(User user, Guid genreId);

        Task<OperationResult<bool>> DeleteLabelAsync(User user, Guid labelId);

        Task<OperationResult<bool>> DeleteReleaseAsync(User user, Guid releaseId, bool? doDeleteFiles);

        Task<OperationResult<bool>> DeleteReleaseSecondaryImageAsync(User user, Guid releaseId, int index);

        Task<OperationResult<bool>> DeleteTracksAsync(User user, IEnumerable<Guid> trackIds, bool? doDeleteFile);

        Task<OperationResult<bool>> DeleteUserAsync(User applicationUser, Guid id);

        Task<OperationResult<bool>> DoInitialSetupAsync(User user, UserManager<User> userManager);

        Task<OperationResult<Dictionary<string, List<string>>>> MissingCollectionReleasesAsync(User user);

        void PerformStartUpTasks();

        Task<OperationResult<bool>> ScanAllCollectionsAsync(User user, bool isReadOnly = false, bool doPurgeFirst = false);

        Task<OperationResult<bool>> ScanArtistAsync(User user, Guid artistId, bool isReadOnly = false);

        Task<OperationResult<bool>> ScanArtistsAsync(User user, IEnumerable<Guid> artistIds, bool isReadOnly = false);

        Task<OperationResult<bool>> ScanCollectionAsync(User user, Guid collectionId, bool isReadOnly = false, bool doPurgeFirst = false, bool doUpdateRanks = true);

        Task<OperationResult<bool>> ScanInboundFolderAsync(User user, bool isReadOnly = false);

        Task<OperationResult<bool>> ScanLibraryFolderAsync(User user, bool isReadOnly = false);

        Task<OperationResult<bool>> ScanReleaseAsync(User user, Guid releaseIds, bool isReadOnly = false, bool wasDoneForInvalidTrackPlay = false);

        Task<OperationResult<bool>> ScanReleasesAsync(User user, IEnumerable<Guid> releaseId, bool isReadOnly = false, bool wasDoneForInvalidTrackPlay = false);

        Task<OperationResult<bool>> ScanLastGiveNumberOfReleasesAsync(User user, int count, bool isReadOnly = false, bool wasDoneForInvalidTrackPlay = false);

        Task<OperationResult<bool>> UpdateInviteTokenUsedAsync(Guid? tokenId);

        Task<OperationResult<bool>> ValidateInviteTokenAsync(Guid? tokenId);
    }
}