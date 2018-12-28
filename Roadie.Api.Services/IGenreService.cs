using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IGenreService
    {
        Task<PagedResult<GenreList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false);
    }
}