using Microsoft.Net.Http.Headers;
using Roadie.Library;
using Roadie.Library.Models.Users;
using System;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IImageService
    {
        Task<FileOperationResult<Library.Models.Image>> ArtistThumbnail(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Library.Models.Image>> CollectionThumbnail(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<OperationResult<bool>> Delete(User roadieUser, Guid id);

        Task<FileOperationResult<Library.Models.Image>> ImageById(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Library.Models.Image>> LabelThumbnail(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Library.Models.Image>> PlaylistThumbnail(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Library.Models.Image>> ReleaseThumbnail(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Library.Models.Image>> TrackThumbnail(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Library.Models.Image>> UserThumbnail(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);
    }
}