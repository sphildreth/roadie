using Microsoft.AspNetCore.Http;
using Roadie.Library;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IGenreService
    {
        Task<OperationResult<Genre>> ByIdAsync(Library.Models.Users.User roadieUser, Guid id, IEnumerable<string> includes = null);

        Task<OperationResult<bool>> DeleteAsync(Library.Identity.User user, Guid id);

        Task<PagedResult<GenreList>> ListAsync(Library.Models.Users.User roadieUser, PagedRequest request, bool? doRandomize = false);

        Task<OperationResult<Image>> SetGenreImageByUrlAsync(Library.Models.Users.User user, Guid id, string imageUrl);

        Task<OperationResult<bool>> UpdateGenreAsync(Library.Models.Users.User user, Genre model);

        Task<OperationResult<Image>> UploadGenreImageAsync(Library.Models.Users.User user, Guid id, IFormFile file);
    }
}