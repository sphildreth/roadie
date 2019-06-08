using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Identity;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("images")]
    [ApiController]
    //  [Authorize]
    public class ImageController : EntityControllerBase
    {
        private IImageService ImageService { get; }

        public ImageController(IImageService imageService, ILoggerFactory logger, ICacheManager cacheManager, 
                               UserManager<ApplicationUser> userManager, IRoadieSettings roadieSettings)
            : base(cacheManager, roadieSettings, userManager)
        {
            this.Logger = logger.CreateLogger("RoadieApi.Controllers.ImageController");
            this.ImageService = imageService;
        }

        [HttpGet("artist/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ArtistImage(Guid id, int? width, int? height)
        {
            var result = await this.ImageService.ArtistImage(id, width ?? this.RoadieSettings.ThumbnailImageSize.Width, height ?? this.RoadieSettings.ThumbnailImageSize.Height);
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

        [HttpGet("artist-secondary/{id}/{imageId}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ArtistSecondaryImage(Guid id, int imageId, int? width, int? height)
        {
            var result = await this.ImageService.ArtistSecondaryImage(id, imageId, width ?? this.RoadieSettings.MaximumImageSize.Width, height ?? this.RoadieSettings.MaximumImageSize.Height);
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

        [HttpGet("collection/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CollectionImage(Guid id, int? width, int? height)
        {
            var result = await this.ImageService.CollectionImage(id, width ?? this.RoadieSettings.ThumbnailImageSize.Width, height ?? this.RoadieSettings.ThumbnailImageSize.Height);
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

        [HttpPost("delete/{id}")]
        [Authorize(Policy = "Editor")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await this.ImageService.Delete(await this.CurrentUserModel(), id);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpGet("{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, int? width, int? height)
        {
            var result = await this.ImageService.ById(id, width, height);
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

        [HttpGet("label/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> LabelImage(Guid id, int? width, int? height)
        {
            var result = await this.ImageService.LabelImage(id, width ?? this.RoadieSettings.ThumbnailImageSize.Width, height ?? this.RoadieSettings.ThumbnailImageSize.Height);
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

        [HttpGet("playlist/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PlaylistImage(Guid id, int? width, int? height)
        {
            var result = await this.ImageService.PlaylistImage(id, width ?? this.RoadieSettings.ThumbnailImageSize.Width, height ?? this.RoadieSettings.ThumbnailImageSize.Height);
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

        [HttpGet("release/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReleaseImage(Guid id, int? width, int? height)
        {
            var result = await this.ImageService.ReleaseImage(id, width ?? this.RoadieSettings.ThumbnailImageSize.Width, height ?? this.RoadieSettings.ThumbnailImageSize.Height);
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

        [HttpGet("release-secondary/{id}/{imageId}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReleaseSecondaryImage(Guid id, int imageId, int? width, int? height)
        {
            var result = await this.ImageService.ReleaseSecondaryImage(id, imageId, width ?? this.RoadieSettings.MaximumImageSize.Width, height ?? this.RoadieSettings.MaximumImageSize.Height);
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

        [HttpPost("search/artist/{query}/{resultsCount:int?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SearchForArtistImage(string query, int? resultsCount)
        {
            var result = await this.ImageService.Search(query, resultsCount ?? 10);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("search/label/{query}/{resultsCount:int?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SearchForLabelImage(string query, int? resultsCount)
        {
            var result = await this.ImageService.Search(query, resultsCount ?? 10);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("search/release/{query}/{resultsCount:int?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SearchForReleaseCover(string query, int? resultsCount)
        {
            var result = await this.ImageService.Search(query, resultsCount ?? 10);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpGet("track/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> TrackImage(Guid id, int? width, int? height)
        {
            var result = await this.ImageService.TrackImage(id, width ?? this.RoadieSettings.ThumbnailImageSize.Width, height ?? this.RoadieSettings.ThumbnailImageSize.Height);
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

        /// <summary>
        /// NOTE that user images/avatars are PNG not JPG this is so it looks better in the menus/applications
        /// </summary>
        [HttpGet("user/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UserImage(Guid id, int? width, int? height)
        {
            var result = await this.ImageService.UserImage(id, width ?? this.RoadieSettings.ThumbnailImageSize.Width, height ?? this.RoadieSettings.ThumbnailImageSize.Height);
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
                        fileDownloadName: $"{ result.Data.Caption ?? id.ToString()}.png",
                        lastModified: result.LastModified,
                        entityTag: result.ETag);
        }
    }
}