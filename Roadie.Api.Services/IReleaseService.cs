using Microsoft.AspNetCore.Http;
using Roadie.Library;
using Roadie.Library.Identity;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Releases;
using Roadie.Library.Models.Users;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IReleaseService
    {
        IEnumerable<int> AddedTrackIds { get; }

        Task<OperationResult<Release>> ById(User roadieUser, Guid id, IEnumerable<string> includes = null);

        Task<OperationResult<bool>> Delete(ApplicationUser user, Library.Data.Release release, bool doDeleteFiles = false, bool doUpdateArtistCounts = true);

        Task<OperationResult<bool>> DeleteReleases(ApplicationUser user, IEnumerable<Guid> releaseIds, bool doDeleteFiles = false);

        Task<PagedResult<ReleaseList>> List(User user, PagedRequest request, bool? doRandomize = false, IEnumerable<string> includes = null);

        Task<OperationResult<bool>> MergeReleases(ApplicationUser user, Guid releaseToMergeId, Guid releaseToMergeIntoId, bool addAsMedia);

        Task<OperationResult<bool>> MergeReleases(ApplicationUser user, Library.Data.Release releaseToMerge, Library.Data.Release releaseToMergeInto, bool addAsMedia);

        Task<FileOperationResult<byte[]>> ReleaseZipped(User roadieUser, Guid id);

        Task<OperationResult<bool>> ScanReleaseFolder(ApplicationUser user, Guid releaseId, bool doJustInfo, Library.Data.Release releaseToScan = null);

        Task<OperationResult<Image>> SetReleaseImageByUrl(ApplicationUser user, Guid id, string imageUrl);

        Task<OperationResult<bool>> UpdateRelease(ApplicationUser user, Release release, string originalReleaseFolder = null);

        Task<OperationResult<Image>> UploadReleaseImage(ApplicationUser user, Guid id, IFormFile file);
    }
}