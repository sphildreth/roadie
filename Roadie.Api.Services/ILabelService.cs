using Microsoft.AspNetCore.Http;
using Roadie.Library;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface ILabelService
    {
        Task<OperationResult<Label>> ByIdAsync(Library.Models.Users.User roadieUser, Guid id, IEnumerable<string> includes = null);

        Task<OperationResult<bool>> DeleteAsync(Library.Identity.User user, Guid id);

        Task<PagedResult<LabelList>> ListAsync(Library.Models.Users.User roadieUser, PagedRequest request, bool? doRandomize = false);

        Task<OperationResult<bool>> MergeLabelsIntoLabelAsync(Library.Identity.User user, Guid intoLabelId, IEnumerable<Guid> labelIdsToMerge);

        Task<OperationResult<Image>> SetLabelImageByUrlAsync(Library.Models.Users.User user, Guid id, string imageUrl);

        Task<OperationResult<bool>> UpdateLabelAsync(Library.Models.Users.User user, Label label);

        Task<OperationResult<Image>> UploadLabelImageAsync(Library.Models.Users.User user, Guid id, IFormFile file);
    }
}