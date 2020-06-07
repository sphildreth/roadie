using Roadie.Library;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using Roadie.Library.Scrobble;
using System;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IPlayActivityService
    {
        Task<PagedResult<PlayActivityList>> ListAsync(PagedRequest request, User roadieUser = null, DateTime? newerThan = null);

        Task<OperationResult<bool>> NowPlayingAsync(User roadieUser, ScrobbleInfo scrobble);

        Task<OperationResult<bool>> ScrobbleAsync(User roadieUser, ScrobbleInfo scrobble);
    }
}