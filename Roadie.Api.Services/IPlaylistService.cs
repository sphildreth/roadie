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
        Task<OperationResult<PlaylistList>> AddNewPlaylist(User user, Playlist model);

        Task<OperationResult<bool>> AddTracksToPlaylist(data.Playlist playlist, IEnumerable<Guid> trackIds);

        Task<OperationResult<Playlist>> ById(User roadieUser, Guid id, IEnumerable<string> includes = null);

        Task<OperationResult<bool>> DeletePlaylist(User user, Guid id);

        Task<PagedResult<PlaylistList>> List(PagedRequest request, User roadieUser = null);

        Task<OperationResult<bool>> ReorderPlaylist(data.Playlist playlist);

        Task<OperationResult<bool>> UpdatePlaylist(User user, Playlist label);

        Task<OperationResult<bool>> UpdatePlaylistTracks(User user, PlaylistTrackModifyRequest request);
    }
}