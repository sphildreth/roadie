using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
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
        public ImageService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext context,
                             ICacheManager cacheManager,
                             ILogger<ArtistService> logger)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
        }

        public async Task<FileOperationResult<Image>> ImageById(Guid id, int? width, int? height, EntityTagHeaderValue etag = null)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();
            var cacheKey = string.Format("urn:image_by_id_operation:{0}", id);
            var result = await this.CacheManager.GetAsync<FileOperationResult<Image>>(cacheKey, async () =>
            {
                return await this.ImageByIdAction(id, etag);
            }, data.Image.CacheRegionKey(id));

            if (result.ETag == etag)
            {
                return new FileOperationResult<Image>(OperationMessages.NotModified);
            }
            sw.Stop();
            return new FileOperationResult<Image>(result.Messages)
            {
                Data = result?.Data,
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
                var sw = new Stopwatch();
                sw.Start();

                var image = this.DbContext.Images
                                          .Include("Release")
                                          .Include("Artist")
                                          .FirstOrDefault(x => x.RoadieId == id);
                if (image == null)
                {
                    return new FileOperationResult<Image>(string.Format("ImageById Not Found [{0}]", id));
                }
                var imageEtag = EtagHelper.GenerateETag(this.HttpEncoder, image.Bytes);
                if (EtagHelper.CompareETag(this.HttpEncoder, etag, imageEtag))
                {
                    return new FileOperationResult<Image>(OperationMessages.NotModified);
                }
                if (!image?.Bytes?.Any() ?? false)
                {
                    return new FileOperationResult<Image>(string.Format("ImageById Not Set [{0}]", id));
                }
                sw.Stop();
                return new FileOperationResult<Image>(image?.Bytes?.Any() ?? false ? OperationMessages.OkMessage : OperationMessages.NoImageDataFound)
                {
                    IsSuccess = true,
                    Data = image.Adapt<Image>(),
                    ContentType = "image/jpeg",
                    LastModified = (image.LastUpdated ?? image.CreatedDate),
                    ETag = imageEtag
                };
            }
            catch (Exception ex)
            {
                this.Logger.LogError($"Error fetching Image [{ id }]", ex);
            }
            return new FileOperationResult<Image>(OperationMessages.ErrorOccured);
        }
    }
}