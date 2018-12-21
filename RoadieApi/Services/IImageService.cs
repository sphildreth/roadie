using Microsoft.Net.Http.Headers;
using Roadie.Library;
using Roadie.Library.Models.Users;
using Roadie.Library.SearchEngines.Imaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IImageService
    {
        Task<FileOperationResult<Library.Models.Image>> ArtistImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Library.Models.Image>> ById(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Library.Models.Image>> CollectionImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<OperationResult<bool>> Delete(User user, Guid id);

        Task<OperationResult<IEnumerable<ImageSearchResult>>> ImageProvidersSearch(string query);

        Task<FileOperationResult<Library.Models.Image>> LabelImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Library.Models.Image>> PlaylistImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Library.Models.Image>> ReleaseImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Library.Models.Image>> TrackImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Library.Models.Image>> UserImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<OperationResult<IEnumerable<ImageSearchResult>>> Search(string query, int resultsCount = 10);
    }
}