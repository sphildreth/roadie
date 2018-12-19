using Microsoft.AspNetCore.Identity;
using Roadie.Library;
using Roadie.Library.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IAdminService
    {
        Task<OperationResult<bool>> DoInitialSetup(ApplicationUser user, UserManager<ApplicationUser> userManager);
        Task<OperationResult<bool>> ScanInboundFolder(ApplicationUser user, bool isReadOnly = false);
        Task<OperationResult<bool>> ScanArtist(ApplicationUser user, Guid artistId, bool isReadOnly = false);
        Task<OperationResult<bool>> ScanRelease(ApplicationUser user, Guid releaseId, bool isReadOnly = false);
        Task<OperationResult<bool>> DeleteRelease(ApplicationUser user, Guid releaseId, bool? doDeleteFiles);
        Task<OperationResult<bool>> DeleteArtist(ApplicationUser user, Guid artistId);
    }
}
