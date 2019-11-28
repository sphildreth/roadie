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
        Task<OperationResult<Artist>> ById(Library.Models.Users.User user, Guid id, IEnumerable<string> includes);

        Task<OperationResult<bool>> Delete(Library.Identity.User user, Library.Data.Artist Artist, bool deleteFolder);

        Task<PagedResult<ArtistList>> List(Library.Models.Users.User user, PagedRequest request, bool? doRandomize = false, bool? onlyIncludeWithReleases = true);

        Task<OperationResult<bool>> MergeArtists(Library.Identity.User user, Guid artistToMergeId, Guid artistToMergeIntoId);

        Task<OperationResult<bool>> RefreshArtistMetadata(Library.Identity.User user, Guid ArtistId);

        Task<OperationResult<bool>> ScanArtistReleasesFolders(Library.Identity.User user, Guid artistId, string destinationFolder, bool doJustInfo);

        Task<OperationResult<Image>> SetReleaseImageByUrl(Library.Identity.User user, Guid id, string imageUrl);

        Task<OperationResult<bool>> UpdateArtist(Library.Identity.User user, Artist artist);

        Task<OperationResult<Image>> UploadArtistImage(Library.Identity.User user, Guid id, IFormFile file);
    }
}