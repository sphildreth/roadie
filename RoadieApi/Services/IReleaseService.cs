using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Roadie.Library;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Releases;
using Roadie.Library.Models.Users;

namespace Roadie.Api.Services
{
    public interface IReleaseService
    {
        Task<PagedResult<ReleaseList>> List(User user, PagedRequest request, bool? doRandomize = false, IEnumerable<string> includes = null);
        Task<OperationResult<Release>> ById(User roadieUser, Guid id, IEnumerable<string> includes = null);
    }
}