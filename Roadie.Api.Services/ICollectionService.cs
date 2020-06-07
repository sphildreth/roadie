using Roadie.Library;
using Roadie.Library.Models.Collections;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ICollectionService
    {
        OperationResult<Collection> Add(User roadieUser);

        Task<OperationResult<Collection>> ByIdAsync(User roadieUser, Guid id, IEnumerable<string> includes = null);

        Task<OperationResult<bool>> DeleteCollectionAsync(User user, Guid id);

        Task<PagedResult<CollectionList>> ListAsync(User roadieUser, PagedRequest request, bool? doRandomize = false, Guid? releaseId = null, Guid? artistId = null);

        Task<OperationResult<bool>> UpdateCollectionAsync(User roadieUser, Collection collection);
    }
}