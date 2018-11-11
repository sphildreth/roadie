using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
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

        public async Task<FileOperationResult<Image>> ArtistThumbnail(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await this.GetImageFileOperation(type: "ArtistThumbnail",
                                                    regionUrn: data.Artist.CacheRegionUrn(id),
                                                    id: id,
                                                    width: width,
                                                    height: height,
                                                    action: async () =>
                                                    {
                                                        return await this.ArtistThumbnailAction(id, etag);
                                                    },
                                                    etag: etag);
        }

        public async Task<FileOperationResult<Image>> ReleaseThumbnail(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await this.GetImageFileOperation(type: "ReleaseThumbnail",
                                                    regionUrn: data.Release.CacheRegionUrn(id),
                                                    id: id,
                                                    width: width,
                                                    height: height,
                                                    action: async () =>
                                                    {
                                                        return await this.ReleaseThumbnailAction(id, etag);
                                                    },
                                                    etag: etag);
        }

        public async Task<FileOperationResult<Image>> ImageById(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            return await this.GetImageFileOperation(type: "ImageById",
                                                    regionUrn: data.Image.CacheRegionUrn(id),
                                                    id: id,
                                                    width: width,
                                                    height: height,
                                                    action: async () =>
                                                    {
                                                        return await this.ImageByIdAction(id, etag);
                                                    },
                                                    etag: etag);
        }

        private async Task<FileOperationResult<Image>> ArtistThumbnailAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var artist = this.GetArtist(id);
                if (artist == null)
                {
                    return new FileOperationResult<Image>(true, string.Format("Artist Not Found [{0}]", id));
                }
                var image = new data.Image
                {
                    Bytes = artist.Thumbnail,
                    CreatedDate = artist.CreatedDate,
                    LastUpdated = artist.LastUpdated
                };
                if (artist.Thumbnail == null || !artist.Thumbnail.Any())
                {
                    image = this.DefaultNotFoundImages.Artist;
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                this.Logger.LogError($"Error fetching Artist Thumbnail [{ id }]", ex);
            }
            return new FileOperationResult<Image>(OperationMessages.ErrorOccured);
        }

        private async Task<FileOperationResult<Image>> ReleaseThumbnailAction(Guid id, EntityTagHeaderValue etag = null)
        {
            try
            {
                var release = this.GetRelease(id);
                if (release == null)
                {
                    return new FileOperationResult<Image>(true, string.Format("Release Not Found [{0}]", id));
                }
                var image = new data.Image
                {
                    Bytes = release.Thumbnail,
                    CreatedDate = release.CreatedDate,
                    LastUpdated = release.LastUpdated
                };
                if (release.Thumbnail == null || !release.Thumbnail.Any())
                {
                    image = this.DefaultNotFoundImages.Release;
                }
                return GenerateFileOperationResult(id, image, etag);
            }
            catch (Exception ex)
            {
                this.Logger.LogError($"Error fetching Release Thumbnail [{ id }]", ex);
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

        private async Task<FileOperationResult<Image>> GetImageFileOperation(string type, string regionUrn, Guid id, int? width, int? height, Func<Task<FileOperationResult<Image>>> action, EntityTagHeaderValue etag = null)
        {
            var sw = Stopwatch.StartNew();
            var result = (await this.CacheManager.GetAsync($"urn:{ type }_by_id_operation:{id}", action, regionUrn)).Adapt<FileOperationResult<Image>>();
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
                if (width.Value != this.Configuration.Thumbnails.Width || height.Value != this.Configuration.Thumbnails.Height)
                {
                    this.Logger.LogInformation($"{ type }: Resized [{ id }], Width [{ width.Value }], Height [{ height.Value }]");
                }
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
    }
}