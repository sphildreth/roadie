using Microsoft.AspNetCore.Identity;
using Roadie.Library;
using Roadie.Library.Identity;
using System;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IAdminService
    {
        Task<OperationResult<bool>> DeleteArtist(ApplicationUser user, Guid artistId);

        Task<OperationResult<bool>> DeleteArtistReleases(ApplicationUser user, Guid artistId, bool doDeleteFiles = false);

        Task<OperationResult<bool>> DeleteRelease(ApplicationUser user, Guid releaseId, bool? doDeleteFiles);

        Task<OperationResult<bool>> DeleteTrack(ApplicationUser user, Guid trackId, bool? doDeleteFile);

        Task<OperationResult<bool>> DoInitialSetup(ApplicationUser user, UserManager<ApplicationUser> userManager);

        Task<OperationResult<bool>> ScanAllCollections(ApplicationUser user, bool isReadOnly = false, bool doPurgeFirst = true);

        Task<OperationResult<bool>> ScanArtist(ApplicationUser user, Guid artistId, bool isReadOnly = false);

        Task<OperationResult<bool>> ScanCollection(ApplicationUser user, Guid collectionId, bool isReadOnly = false, bool doPurgeFirst = true);

        Task<OperationResult<bool>> ScanInboundFolder(ApplicationUser user, bool isReadOnly = false);

        Task<OperationResult<bool>> ScanLibraryFolder(ApplicationUser user, bool isReadOnly = false);

        Task<OperationResult<bool>> ScanRelease(ApplicationUser user, Guid releaseId, bool isReadOnly = false, bool wasDoneForInvalidTrackPlay = false);
    }
}