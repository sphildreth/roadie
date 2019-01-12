using Microsoft.AspNetCore.Http;
using Roadie.Library;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Users;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IArtistService
    {
        Task<OperationResult<Artist>> ById(User roadieUser, Guid id, IEnumerable<string> includes);

        Task<PagedResult<ArtistList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false, bool? onlyIncludeWithReleases = true);

        Task<OperationResult<Library.Models.Image>> SetReleaseImageByUrl(User user, Guid id, string imageUrl);

        Task<OperationResult<bool>> UpdateArtist(User user, Artist artist);

        Task<OperationResult<Library.Models.Image>> UploadArtistImage(User user, Guid id, IFormFile file);

        Task<OperationResult<bool>> MergeArtists(User user, Guid artistToMergeId, Guid artistToMergeIntoId);
    }
}