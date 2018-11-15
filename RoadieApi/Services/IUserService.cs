using System.Threading.Tasks;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;

namespace Roadie.Api.Services
{
    public interface IUserService
    {
        Task<PagedResult<UserList>> List(PagedRequest request);
    }
}