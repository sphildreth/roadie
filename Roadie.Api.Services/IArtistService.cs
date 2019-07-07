using Microsoft.AspNetCore.Http;
using Roadie.Library;
using Roadie.Library.Identity;
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
        Task<OperationResult<Artist>> ById(User user, Guid id, IEnumerable<string> includes);

        Task<OperationResult<bool>> Delete(ApplicationUser user, Library.Data.Artist Artist);

        Task<PagedResult<ArtistList>> List(User user, PagedRequest request, bool? doRandomize = false, bool? onlyIncludeWithReleases = true);

        Task<OperationResult<bool>> MergeArtists(ApplicationUser user, Guid artistToMergeId, Guid artistToMergeIntoId);

        Task<OperationResult<bool>> RefreshArtistMetadata(ApplicationUser user, Guid ArtistId);

        Task<OperationResult<bool>> ScanArtistReleasesFolders(ApplicationUser user, Guid artistId, string destinationFolder, bool doJustInfo);

        Task<OperationResult<Image>> SetReleaseImageByUrl(ApplicationUser user, Guid id, string imageUrl);

        Task<OperationResult<bool>> UpdateArtist(ApplicationUser user, Artist artist);

        Task<OperationResult<Image>> UploadArtistImage(ApplicationUser user, Guid id, IFormFile file);
    }
}