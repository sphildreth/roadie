using Microsoft.Net.Http.Headers;
using Roadie.Library;
using Roadie.Library.Imaging;
using Roadie.Library.SearchEngines.Imaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IImageService
    {
        string Referrer { get; set; }

        string RequestIp { get; set; }

        Task<FileOperationResult<IImage>> ArtistImageAsync(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<IImage>> ArtistSecondaryImageAsync(Guid id, int imageId, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<IImage>> CollectionImageAsync(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<IImage>> GenreImageAsync(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<IImage>> LabelImageAsync(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<IImage>> PlaylistImageAsync(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<IImage>> ReleaseImageAsync(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<IImage>> ReleaseSecondaryImageAsync(Guid id, int imageId, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<OperationResult<IEnumerable<ImageSearchResult>>> SearchAsync(string query, int resultsCount = 10);

        Task<FileOperationResult<IImage>> TrackImageAsync(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<IImage>> UserImageAsync(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);
    }
}