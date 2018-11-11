using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("image")]
    [ApiController]
    [Authorize]
    public class ImageController : EntityControllerBase
    {
        private IImageService ImageService { get; }

        public ImageController(IImageService imageService, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration)
            : base(cacheManager, configuration)
        {
            this._logger = logger.CreateLogger("RoadieApi.Controllers.ImageController");
            this.ImageService = imageService;
        }

        //[EnableQuery]
        //public IActionResult Get()
        //{
        //    return Ok(this._RoadieDbContext.Tracks.ProjectToType<models.Image>());
        //}

        [HttpGet("thumbnail/artist/{id}/{width:int?}/{height:int?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ArtistThumbnail(Guid id, int? width, int? height)
        {
            var result = await this.ImageService.ArtistThumbnail(id, width ?? this.RoadieSettings.Thumbnails.Width, height ?? this.RoadieSettings.Thumbnails.Height);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return File(fileContents: result.Data.Bytes,
                        contentType: result.ContentType,
                        fileDownloadName: $"{ result.Data.Caption ?? id.ToString()}.jpg",
                        lastModified: result.LastModified,
                        entityTag: result.ETag);
        }

        [HttpGet("thumbnail/collection/{id}/{width:int?}/{height:int?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CollectionThumbnail(Guid id, int? width, int? height)
        {
            var result = await this.ImageService.CollectionThumbnail(id, width ?? this.RoadieSettings.Thumbnails.Width, height ?? this.RoadieSettings.Thumbnails.Height);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return File(fileContents: result.Data.Bytes,
                        contentType: result.ContentType,
                        fileDownloadName: $"{ result.Data.Caption ?? id.ToString()}.jpg",
                        lastModified: result.LastModified,
                        entityTag: result.ETag);
        }

        [HttpGet("{id}/{width:int?}/{height:int?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, int? width, int? height)
        {
            var result = await this.ImageService.ImageById(id, width, height);
            if (result == null)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return File(fileContents: result.Data.Bytes,
                        contentType: result.ContentType,
                        fileDownloadName: $"{ result.Data.Caption ?? id.ToString()}.jpg",
                        lastModified: result.LastModified,
                        entityTag: result.ETag);
        }

        [HttpGet("thumbnail/label/{id}/{width:int?}/{height:int?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> LabelThumbnail(Guid id, int? width, int? height)
        {
            var result = await this.ImageService.LabelThumbnail(id, width ?? this.RoadieSettings.Thumbnails.Width, height ?? this.RoadieSettings.Thumbnails.Height);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return File(fileContents: result.Data.Bytes,
                        contentType: result.ContentType,
                        fileDownloadName: $"{ result.Data.Caption ?? id.ToString()}.jpg",
                        lastModified: result.LastModified,
                        entityTag: result.ETag);
        }

        [HttpGet("thumbnail/playlist/{id}/{width:int?}/{height:int?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PlaylistThumbnail(Guid id, int? width, int? height)
        {
            var result = await this.ImageService.PlaylistThumbnail(id, width ?? this.RoadieSettings.Thumbnails.Width, height ?? this.RoadieSettings.Thumbnails.Height);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return File(fileContents: result.Data.Bytes,
                        contentType: result.ContentType,
                        fileDownloadName: $"{ result.Data.Caption ?? id.ToString()}.jpg",
                        lastModified: result.LastModified,
                        entityTag: result.ETag);
        }

        [HttpGet("thumbnail/release/{id}/{width:int?}/{height:int?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReleaseThumbnail(Guid id, int? width, int? height)
        {
            var result = await this.ImageService.ReleaseThumbnail(id, width ?? this.RoadieSettings.Thumbnails.Width, height ?? this.RoadieSettings.Thumbnails.Height);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return File(fileContents: result.Data.Bytes,
                        contentType: result.ContentType,
                        fileDownloadName: $"{ result.Data.Caption ?? id.ToString()}.jpg",
                        lastModified: result.LastModified,
                        entityTag: result.ETag);
        }

        [HttpGet("thumbnail/track/{id}/{width:int?}/{height:int?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> TrackThumbnail(Guid id, int? width, int? height)
        {
            var result = await this.ImageService.TrackThumbnail(id, width ?? this.RoadieSettings.Thumbnails.Width, height ?? this.RoadieSettings.Thumbnails.Height);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return File(fileContents: result.Data.Bytes,
                        contentType: result.ContentType,
                        fileDownloadName: $"{ result.Data.Caption ?? id.ToString()}.jpg",
                        lastModified: result.LastModified,
                        entityTag: result.ETag);
        }

        [HttpGet("thumbnail/user/{id}/{width:int?}/{height:int?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UserThumbnail(Guid id, int? width, int? height)
        {
            var result = await this.ImageService.UserThumbnail(id, width ?? this.RoadieSettings.Thumbnails.Width, height ?? this.RoadieSettings.Thumbnails.Height);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return File(fileContents: result.Data.Bytes,
                        contentType: result.ContentType,
                        fileDownloadName: $"{ result.Data.Caption ?? id.ToString()}.jpg",
                        lastModified: result.LastModified,
                        entityTag: result.ETag);
        }
    }
}