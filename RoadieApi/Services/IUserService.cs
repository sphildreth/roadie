using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IUserService
    {
        Task<PagedResult<UserList>> List(PagedRequest request);
    }
}