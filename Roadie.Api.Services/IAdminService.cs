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
        Task<OperationResult<bool>> DeleteArtist(ApplicationUser user, Guid artistId, bool deleteFolder);

        Task<OperationResult<bool>> DeleteArtistReleases(ApplicationUser user, Guid artistId, bool doDeleteFiles = false);

        Task<OperationResult<bool>> DeleteArtistSecondaryImage(ApplicationUser user, Guid artistId, int index);

        Task<OperationResult<bool>> DeleteGenre(ApplicationUser user, Guid genreId);

        Task<OperationResult<bool>> DeleteLabel(ApplicationUser user, Guid labelId);

        Task<OperationResult<bool>> DeleteRelease(ApplicationUser user, Guid releaseId, bool? doDeleteFiles);

        Task<OperationResult<bool>> DeleteReleaseSecondaryImage(ApplicationUser user, Guid releaseId, int index);

        Task<OperationResult<bool>> DeleteTracks(ApplicationUser user, IEnumerable<Guid> trackIds, bool? doDeleteFile);

        Task<OperationResult<bool>> DeleteUser(ApplicationUser applicationUser, Guid id);

        Task<OperationResult<bool>> DoInitialSetup(ApplicationUser user, UserManager<ApplicationUser> userManager);

        Task<OperationResult<Dictionary<string, List<string>>>> MissingCollectionReleases(ApplicationUser user);

        void PerformStartUpTasks();

        Task<OperationResult<bool>> ScanAllCollections(ApplicationUser user, bool isReadOnly = false, bool doPurgeFirst = false);

        Task<OperationResult<bool>> ScanArtist(ApplicationUser user, Guid artistId, bool isReadOnly = false);

        Task<OperationResult<bool>> ScanArtists(ApplicationUser user, IEnumerable<Guid> artistIds, bool isReadOnly = false);

        Task<OperationResult<bool>> ScanCollection(ApplicationUser user, Guid collectionId, bool isReadOnly = false, bool doPurgeFirst = false, bool doUpdateRanks = true);

        Task<OperationResult<bool>> ScanInboundFolder(ApplicationUser user, bool isReadOnly = false);

        Task<OperationResult<bool>> ScanLibraryFolder(ApplicationUser user, bool isReadOnly = false);

        Task<OperationResult<bool>> ScanRelease(ApplicationUser user, Guid releaseIds, bool isReadOnly = false, bool wasDoneForInvalidTrackPlay = false);

        Task<OperationResult<bool>> ScanReleases(ApplicationUser user, IEnumerable<Guid> releaseId, bool isReadOnly = false, bool wasDoneForInvalidTrackPlay = false);

        Task<OperationResult<bool>> UpdateInviteTokenUsed(Guid? tokenId);

        Task<OperationResult<bool>> ValidateInviteToken(Guid? tokenId);

        Task<OperationResult<bool>> MigrateImages(ApplicationUser user);

        Task<OperationResult<bool>> MigrateStorage(ApplicationUser user, bool deleteEmptyFolders);
    }
}