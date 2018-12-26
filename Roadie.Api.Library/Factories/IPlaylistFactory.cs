using Roadie.Library.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Library.Factories
{
    public interface IPlaylistFactory
    {
        Task<OperationResult<bool>> AddTracksToPlaylist(Playlist playlist, IEnumerable<Guid> trackIds);

        Task<OperationResult<bool>> ReorderPlaylist(Playlist playlist);
    }
}