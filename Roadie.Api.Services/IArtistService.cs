using Microsoft.AspNetCore.Http;
using Roadie.Library;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IArtistService
    {
        Task<OperationResult<Artist>> ByIdAsync(Library.Models.Users.User user, Guid id, IEnumerable<string> includes);

        Task<OperationResult<bool>> DeleteAsync(Library.Identity.User user, Library.Data.Artist Artist, bool deleteFolder);

        Task<PagedResult<ArtistList>> ListAsync(Library.Models.Users.User user, PagedRequest request, bool? doRandomize = false, bool? onlyIncludeWithReleases = true);

        Task<OperationResult<bool>> MergeArtistsAsync(Library.Identity.User user, Guid artistToMergeId, Guid artistToMergeIntoId);

        Task<OperationResult<bool>> RefreshArtistMetadataAsync(Library.Identity.User user, Guid ArtistId);

        Task<OperationResult<bool>> ScanArtistReleasesFoldersAsync(Library.Identity.User user, Guid artistId, string destinationFolder, bool doJustInfo);

        Task<OperationResult<Image>> SetReleaseImageByUrlAsync(Library.Identity.User user, Guid id, string imageUrl);

        Task<OperationResult<bool>> UpdateArtistAsync(Library.Identity.User user, Artist artist);

        Task<OperationResult<Image>> UploadArtistImageAsync(Library.Identity.User user, Guid id, IFormFile file);
    }
}