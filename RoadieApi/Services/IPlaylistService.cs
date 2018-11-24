using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Playlists;
using Roadie.Library.Models.Users;
using System;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IPlaylistService
    {
        Task<PagedResult<PlaylistList>> List(PagedRequest request, User roadieUser = null);
    }
}