using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ITrackService
    {
        Task<PagedResult<PlayActivityList>> PlayActivityList(PagedRequest request, User user = null);
    }
}