using System;
using System.Threading.Tasks;
using Roadie.Library.Models.Collections;
using Roadie.Library.Models.Pagination;

namespace Roadie.Api.Services
{
    public interface ICollectionService
    {
        Task<PagedResult<CollectionList>> CollectionList(PagedRequest request, Guid? releaseId = null, Guid? artistId = null);
    }
}