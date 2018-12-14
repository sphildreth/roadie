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
        Task<OperationResult<bool>> ScanInboundFolder(ApplicationUser user, bool isReadOnly = false);
    }
}
