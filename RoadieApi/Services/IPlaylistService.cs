using System;
using System.Threading.Tasks;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Playlists;

namespace Roadie.Api.Services
{
    public interface IPlaylistService
    {
        Task<PagedResult<PlaylistList>> PlaylistList(PagedRequest request, Guid? userId = null, Guid? artistId = null);
    }
}