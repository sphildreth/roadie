using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Extensions;
using Roadie.Library.Imaging;
using Roadie.Library.Models;
using Roadie.Library.Utility;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public class ImageService : ServiceBase, IImageService
    {
        private IDefaultNotFoundImages DefaultNotFoundImages { get; }

        public ImageService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext context,
                             ICacheManager cacheManager,
                             ILogger<ArtistService> logger,
                             IDefaultNotFoundImages defaultNotFoundImages)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
            this.DefaultNotFoundImages = defaultNotFoundImages;
        }

        public async Task<FileOperationResult<Image>> ImageById(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            var sw = Stopwatch.StartNew();
            var result = (await this.CacheManager.GetAsync(data.Image.CacheUrn(id), async () =>
            {
                return await this.ImageByIdAction(id, etag);
            }, data.Image.CacheRegionUrn(id))).Adapt<FileOperationResult<Image>>();
            if (!result.IsSuccess)
            {
                return new FileOperationResult<Image>(result.IsNotFoundResult, result.Messages);
            }
            if (result.ETag == etag)
            {
                return new FileOperationResult<Image>(OperationMessages.NotModified);
            }
            if ((width.HasValue || height.HasValue) && result?.Data?.Bytes != null)
            {
                result.Data.Bytes = ImageHelper.ResizeImage(result?.Data?.Bytes, width.Value, height.Value);
                result.ETag = EtagHelper.GenerateETag(this.HttpEncoder, result.Data.Bytes);
                result.LastModified = DateTime.UtcNow;
                this.Logger.LogInformation($"ImageById: Resized [{ id }], Width [{ width.Value }], Height [{ height.Value }]");
            }
            sw.Stop();
            return new FileOperationResult<Image>(result.Messages)
            {
                Data = result.Data,
                ETag = result.ETag,
                LastModified = result.LastModified,
                ContentType = result.ContentType,
                Errors = result?.Errors,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<FileOperationResult<Image>> ArtistThumbnail(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            var sw = Stopwatch.StartNew();
            var result = (await this.CacheManager.GetAsync($"urn:artist_thumbnail_by_id_operation:{id}", async () =>
            {
                return await this.ArtistThumbnailAction(id, etag);
            }, data.Artist.CacheRegionUrn(id))).Adapt<FileOperationResult<Image>>();
            if(!result.IsSuccess)
            {
                return new FileOperationResult<Image>(result.IsNotFoundResult, result.Messages);
            }
            if (result.ETag == etag)
            {
                return new FileOperationResult<Image>(OperationMessages.NotModified);
            }
            if ((width.HasValue || height.HasValue) && result?.Data?.Bytes != null)
            {
                result.Data.Bytes = ImageHelper.ResizeImage(result?.Data?.Bytes, width.Value, height.Value);
                result.ETag = EtagHelper.GenerateETag(this.HttpEncoder, result.Data.Bytes);
                result.LastModified = DateTime.UtcNow;
                this.Logger.LogInformation($"ArtistThumbnail: Resized [{ id }], Width [{ width.Value }], Height [{ height.Value }]");
            }
            sw.Stop();
            return new FileOperationResult<Image>(result.Messages)
            {
                Data = result.Data,
                ETag = result.ETag,
                LastModified = result.LastModified,
                ContentType = result.ContentType,
                Errors = result?.Errors,
                IsSuccess = result?.IsSuccess ?? false,
                OperationTime = sw.ElapsedMilliseconds
            };
        }


        private async Task<FileOperationResult<Image>> ArtistThumbnailAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var artist = this.GetArtist(id);
                if(artist == null)
                {
                    return new FileOperationResult<Image>(true, string.Format("Artist Not Found [{0}]", id));
                }
                var image = new data.Image
                {
                    Bytes = artist.Thumbnail,
                    CreatedDate = artist.CreatedDate,
                    LastUpdated = artist.LastUpdated
                };
                if(artist.Thumbnail == null || !artist.Thumbnail.Any())
                {
                    image = this.DefaultNotFoundImages.Artist;
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                this.Logger.LogError($"Error fetching Image [{ id }]", ex);
            }
            return new FileOperationResult<Image>(OperationMessages.ErrorOccured);
        }

        private async Task<FileOperationResult<Image>> ImageByIdAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var image = this.DbContext.Images
                                          .Include("Release")
                                          .Include("Artist")
                                          .FirstOrDefault(x => x.RoadieId == id);
                if (image == null)
                {
                    return new FileOperationResult<Image>(true, string.Format("ImageById Not Found [{0}]", id));
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                this.Logger.LogError($"Error fetching Image [{ id }]", ex);
            }
            return new FileOperationResult<Image>(OperationMessages.ErrorOccured);
        }

        private FileOperationResult<Image> GenerateFileOperationResult(Guid id, data.Image image, EntityTagHeaderValue etag = null)
        {
            var imageEtag = EtagHelper.GenerateETag(this.HttpEncoder, image.Bytes);
            if (EtagHelper.CompareETag(this.HttpEncoder, etag, imageEtag))
            {
                return new FileOperationResult<Image>(OperationMessages.NotModified);
            }
            if (!image?.Bytes?.Any() ?? false)
            {
                return new FileOperationResult<Image>(string.Format("ImageById Not Set [{0}]", id));
            }
            return new FileOperationResult<Image>(image?.Bytes?.Any() ?? false ? OperationMessages.OkMessage : OperationMessages.NoImageDataFound)
            {
                IsSuccess = true,
                Data = image.Adapt<Image>(),
                ContentType = "image/jpeg",
                LastModified = (image.LastUpdated ?? image.CreatedDate),
                ETag = imageEtag
            };
        }

    }
}