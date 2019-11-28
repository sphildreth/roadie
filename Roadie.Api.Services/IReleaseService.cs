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

        Task<OperationResult<Release>> ById(Library.Models.Users.User roadieUser, Guid id, IEnumerable<string> includes = null);

        Task<OperationResult<bool>> Delete(Library.Identity.User user, Library.Data.Release release, bool doDeleteFiles = false, bool doUpdateArtistCounts = true);

        Task<OperationResult<bool>> DeleteReleases(Library.Identity.User user, IEnumerable<Guid> releaseIds, bool doDeleteFiles = false);

        Task<PagedResult<ReleaseList>> List(Library.Models.Users.User user, PagedRequest request, bool? doRandomize = false, IEnumerable<string> includes = null);

        Task<OperationResult<bool>> MergeReleases(Library.Identity.User user, Guid releaseToMergeId, Guid releaseToMergeIntoId, bool addAsMedia);

        Task<OperationResult<bool>> MergeReleases(Library.Identity.User user, Library.Data.Release releaseToMerge, Library.Data.Release releaseToMergeInto, bool addAsMedia);

        Task<FileOperationResult<byte[]>> ReleaseZipped(Library.Models.Users.User roadieUser, Guid id);

        Task<OperationResult<bool>> ScanReleaseFolder(Library.Identity.User user, Guid releaseId, bool doJustInfo, Library.Data.Release releaseToScan = null);

        Task<OperationResult<Image>> SetReleaseImageByUrl(Library.Identity.User user, Guid id, string imageUrl);

        Task<OperationResult<bool>> UpdateRelease(Library.Identity.User user, Release release, string originalReleaseFolder = null);

        Task<OperationResult<Image>> UploadReleaseImage(Library.Identity.User user, Guid id, IFormFile file);
    }
}