using Roadie.Library;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Playlists;
using Roadie.Library.Models.Users;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IPlaylistService
    {
        Task<OperationResult<Playlist>> ById(User roadieUser, Guid id, IEnumerable<string> includes = null);

        Task<PagedResult<PlaylistList>> List(PagedRequest request, User roadieUser = null);
    }
}