using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using System;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ITrackService
    {
        Task<Library.Models.Pagination.PagedResult<TrackList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false, Guid? releaseId = null);
    }
}