using Microsoft.Net.Http.Headers;
using Roadie.Library;
using Roadie.Library.Identity;
using Roadie.Library.Imaging;
using Roadie.Library.Models;
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

        Task<FileOperationResult<IImage>> ArtistImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<IImage>> ArtistSecondaryImage(Guid id, int imageId, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<IImage>> CollectionImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<IImage>> GenreImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<IImage>> LabelImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<IImage>> PlaylistImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<IImage>> ReleaseImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<IImage>> ReleaseSecondaryImage(Guid id, int imageId, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<OperationResult<IEnumerable<ImageSearchResult>>> Search(string query, int resultsCount = 10);

        Task<FileOperationResult<IImage>> TrackImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<IImage>> UserImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);
    }
}