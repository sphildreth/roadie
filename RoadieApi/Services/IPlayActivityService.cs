using System.Threading.Tasks;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;

namespace Roadie.Api.Services
{
    public interface IPlayActivityService
    {
        Task<PagedResult<PlayActivityList>> List(PagedRequest request, User roadieUser = null);
    }
}