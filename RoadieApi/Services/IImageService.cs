using System;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Roadie.Library;

namespace Roadie.Api.Services
{
    public interface IImageService
    {
        Task<FileOperationResult<Library.Models.Image>> ImageById(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);
        Task<FileOperationResult<Library.Models.Image>> ArtistThumbnail(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);
    }
}