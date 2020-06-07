using Roadie.Library;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Playlists;
using Roadie.Library.Models.Users;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public interface IPlaylistService
    {
        Task<OperationResult<PlaylistList>> AddNewPlaylistAsync(User user, Playlist model);

        Task<OperationResult<bool>> AddTracksToPlaylistAsync(data.Playlist playlist, IEnumerable<Guid> trackIds);

        Task<OperationResult<Playlist>> ByIdAsync(User roadieUser, Guid id, IEnumerable<string> includes = null);

        Task<OperationResult<bool>> DeletePlaylistAsync(User user, Guid id);

        Task<PagedResult<PlaylistList>> ListAsync(PagedRequest request, User roadieUser = null);

        Task<OperationResult<bool>> ReorderPlaylistAsync(data.Playlist playlist);

        Task<OperationResult<bool>> UpdatePlaylistAsync(User user, Playlist label);

        Task<OperationResult<bool>> UpdatePlaylistTracksAsync(User user, PlaylistTrackModifyRequest request);
    }
}