using Roadie.Library;
using Roadie.Library.Enums;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IBookmarkService
    {
        Task<PagedResult<BookmarkList>> ListAsync(User roadieUser, PagedRequest request, bool? doRandomize = false, BookmarkType? filterType = null);
        Task<OperationResult<bool>> RemoveAllBookmarksForItemAsync(BookmarkType type, int id);
    }
}