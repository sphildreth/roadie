using Roadie.Library.Models.Collections;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using System;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ICollectionService
    {

        Task<PagedResult<CollectionList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false, Guid? releaseId = null, Guid? artistId = null);
    }
}