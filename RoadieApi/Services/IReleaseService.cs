using Roadie.Library;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Releases;
using Roadie.Library.Models.Users;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IReleaseService
    {
        Task<OperationResult<Release>> ById(User roadieUser, Guid id, IEnumerable<string> includes = null);

        Task<PagedResult<ReleaseList>> List(User user, PagedRequest request, bool? doRandomize = false);
        Task<FileOperationResult<byte[]>> ReleaseZipped(User roadieUser, Guid id);
    }
}