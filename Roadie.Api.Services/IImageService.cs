﻿using Microsoft.Net.Http.Headers;
using Roadie.Library;
using Roadie.Library.Models;
using Roadie.Library.Models.Users;
using Roadie.Library.SearchEngines.Imaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public interface IImageService
    {
        Task<FileOperationResult<Image>>
            ArtistImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Image>> ArtistSecondaryImage(Guid id, int imageId, int? width, int? height,
            EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Image>> ById(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Image>> CollectionImage(Guid id, int? width, int? height,
            EntityTagHeaderValue etag = null);

        Task<OperationResult<bool>> Delete(User user, Guid id);

        Task<OperationResult<IEnumerable<ImageSearchResult>>> ImageProvidersSearch(string query);

        Task<FileOperationResult<Image>> LabelImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Image>> PlaylistImage(Guid id, int? width, int? height,
            EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Image>> ReleaseImage(Guid id, int? width, int? height,
            EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Image>> ReleaseSecondaryImage(Guid id, int imageId, int? width, int? height,
            EntityTagHeaderValue etag = null);

        Task<OperationResult<IEnumerable<ImageSearchResult>>> Search(string query, int resultsCount = 10);

        Task<FileOperationResult<Image>> TrackImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);

        Task<FileOperationResult<Image>> UserImage(Guid id, int? width, int? height, EntityTagHeaderValue etag = null);
    }
}