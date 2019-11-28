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
        Task<OperationResult<bool>> DeleteArtist(User user, Guid artistId, bool deleteFolder);

        Task<OperationResult<bool>> DeleteArtistReleases(User user, Guid artistId, bool doDeleteFiles = false);

        Task<OperationResult<bool>> DeleteArtistSecondaryImage(User user, Guid artistId, int index);

        Task<OperationResult<bool>> DeleteGenre(User user, Guid genreId);

        Task<OperationResult<bool>> DeleteLabel(User user, Guid labelId);

        Task<OperationResult<bool>> DeleteRelease(User user, Guid releaseId, bool? doDeleteFiles);

        Task<OperationResult<bool>> DeleteReleaseSecondaryImage(User user, Guid releaseId, int index);

        Task<OperationResult<bool>> DeleteTracks(User user, IEnumerable<Guid> trackIds, bool? doDeleteFile);

        Task<OperationResult<bool>> DeleteUser(User applicationUser, Guid id);

        Task<OperationResult<bool>> DoInitialSetup(User user, UserManager<User> userManager);

        Task<OperationResult<Dictionary<string, List<string>>>> MissingCollectionReleases(User user);

        void PerformStartUpTasks();

        Task<OperationResult<bool>> ScanAllCollections(User user, bool isReadOnly = false, bool doPurgeFirst = false);

        Task<OperationResult<bool>> ScanArtist(User user, Guid artistId, bool isReadOnly = false);

        Task<OperationResult<bool>> ScanArtists(User user, IEnumerable<Guid> artistIds, bool isReadOnly = false);

        Task<OperationResult<bool>> ScanCollection(User user, Guid collectionId, bool isReadOnly = false, bool doPurgeFirst = false, bool doUpdateRanks = true);

        Task<OperationResult<bool>> ScanInboundFolder(User user, bool isReadOnly = false);

        Task<OperationResult<bool>> ScanLibraryFolder(User user, bool isReadOnly = false);

        Task<OperationResult<bool>> ScanRelease(User user, Guid releaseIds, bool isReadOnly = false, bool wasDoneForInvalidTrackPlay = false);

        Task<OperationResult<bool>> ScanReleases(User user, IEnumerable<Guid> releaseId, bool isReadOnly = false, bool wasDoneForInvalidTrackPlay = false);

        Task<OperationResult<bool>> UpdateInviteTokenUsed(Guid? tokenId);

        Task<OperationResult<bool>> ValidateInviteToken(Guid? tokenId);
    }
}