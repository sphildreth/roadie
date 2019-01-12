using Microsoft.AspNetCore.Http;
using Roadie.Library;
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
        Task<OperationResult<Release>> ById(User roadieUser, Guid id, IEnumerable<string> includes = null);

        Task<PagedResult<ReleaseList>> List(User user, PagedRequest request, bool? doRandomize = false, IEnumerable<string> includes = null);

        Task<FileOperationResult<byte[]>> ReleaseZipped(User roadieUser, Guid id);

        Task<OperationResult<Library.Models.Image>> SetReleaseImageByUrl(User user, Guid id, string imageUrl);

        Task<OperationResult<bool>> UpdateRelease(User user, Release release);

        Task<OperationResult<Library.Models.Image>> UploadReleaseImage(User user, Guid id, IFormFile file);

        Task<OperationResult<bool>> MergeReleases(User user, Guid releaseToMergeId, Guid releaseToMergeIntoId, bool addAsMedia);
    }
}