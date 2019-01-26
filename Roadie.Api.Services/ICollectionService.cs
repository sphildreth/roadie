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

        Task<OperationResult<bool>> UpdateCollection(User roadieUser, Collection collection);

        Task<OperationResult<bool>> DeleteCollection(User user, Guid id);

        Task<OperationResult<Collection>> ById(User roadieUser, Guid id, IEnumerable<string> includes = null);

        Task<PagedResult<CollectionList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false, Guid? releaseId = null, Guid? artistId = null);
    }
}