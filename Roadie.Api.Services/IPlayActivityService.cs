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
        Task<PagedResult<PlayActivityList>> List(PagedRequest request, User roadieUser = null,
            DateTime? newerThan = null);

        Task<OperationResult<bool>> NowPlaying(User roadieUser, ScrobbleInfo scrobble);

        Task<OperationResult<bool>> Scrobble(User roadieUser, ScrobbleInfo scrobble);
    }
}