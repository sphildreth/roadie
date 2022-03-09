using Roadie.Library;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ITrackService
    {
        Task<Roadie.Library.Identity.User> GetUserByUserNameAsync(string username);

        Task<OperationResult<Track>> ByIdAsyncAsync(User roadieUser, Guid id, IEnumerable<string> includes);

        Task<PagedResult<TrackList>> ListAsync(PagedRequest request, User roadieUser, bool? doRandomize = false, Guid? releaseId = null);

        OperationResult<Track> StreamCheckAndInfo(User roadieUser, Guid id);

        Task<OperationResult<TrackStreamInfo>> TrackStreamInfoAsync(Guid trackId, long beginBytes, long endBytes, User roadieUser);

        Task<OperationResult<bool>> UpdateTrackAsync(User user, Track track);
    }
}