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
        Task<OperationResult<Track>> ById(User roadieUser, Guid id, IEnumerable<string> includes);

        Task<PagedResult<TrackList>> List(PagedRequest request, User roadieUser, bool? doRandomize = false,
            Guid? releaseId = null);

        OperationResult<Track> StreamCheckAndInfo(User roadieUser, Guid id);

        Task<OperationResult<TrackStreamInfo>> TrackStreamInfo(Guid trackId, long beginBytes, long endBytes,
            User roadieUser);

        Task<OperationResult<bool>> UpdateTrack(User user, Track track);
    }
}