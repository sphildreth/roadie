using Roadie.Library;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using System;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IPlayActivityService
    {
        Task<OperationResult<PlayActivityList>> CreatePlayActivity(User roadieUser, TrackStreamInfo streamInfo);

        Task<PagedResult<PlayActivityList>> List(PagedRequest request, User roadieUser = null, DateTime? newerThan = null);
    }
}