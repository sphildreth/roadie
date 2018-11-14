using Roadie.Library;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IArtistService
    {
        Task<OperationResult<Artist>> ById(User roadieUser, Guid id, IEnumerable<string> includes);

        Task<PagedResult<ArtistList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false);
    }
}