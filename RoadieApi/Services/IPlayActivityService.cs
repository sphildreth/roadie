using Roadie.Library;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IPlayActivityService
    {
        Task<PagedResult<PlayActivityList>> List(PagedRequest request, User roadieUser = null);

        Task<OperationResult<UserTrack>> CreatePlayActivity(User roadieUser, TrackStreamInfo streamInfo);
    }
}