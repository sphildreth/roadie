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
    public interface IGenreService
    {
        Task<OperationResult<Genre>> ById(User roadieUser, Guid id, IEnumerable<string> includes = null);
        Task<OperationResult<bool>> Delete(ApplicationUser user, Guid id);
        Task<PagedResult<GenreList>> List(User roadieUser, PagedRequest request, bool? doRandomize = false);
        Task<OperationResult<Image>> SetGenreImageByUrl(User user, Guid id, string imageUrl);
        Task<OperationResult<Image>> UploadGenreImage(User user, Guid id, IFormFile file);

    }
}