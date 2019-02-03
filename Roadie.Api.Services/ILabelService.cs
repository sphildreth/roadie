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
    public interface ILabelService
    {
        Task<OperationResult<Label>> ById(User roadieUser, Guid id, IEnumerable<string> includes = null);

        Task<PagedResult<LabelList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false);

        Task<OperationResult<Image>> SetLabelImageByUrl(User user, Guid id, string imageUrl);

        Task<OperationResult<bool>> UpdateLabel(User user, Label label);

        Task<OperationResult<Image>> UploadLabelImage(User user, Guid id, IFormFile file);
    }
}