using Roadie.Library;
using Roadie.Library.Identity;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using System;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IUserService
    {
        Task<PagedResult<UserList>> List(PagedRequest request);
        Task<OperationResult<bool>> SetReleaseRating(Guid releaseId, User roadieUser, short rating);
        Task<OperationResult<bool>> SetArtistRating(Guid artistId, User roadieUser, short rating);
        Task<OperationResult<bool>> SetTrackRating(Guid trackId, User roadieUser, short rating);
    }
}