using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
            Logger = logger.CreateLogger("RoadieApi.Controllers.ImageController");
            ImageService = imageService;
        }

        [HttpGet("artist/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ArtistImage(Guid id, int? width, int? height)
        {
            var result = await ImageService.ArtistImage(id, width ?? RoadieSettings.ThumbnailImageSize.Width,
                height ?? RoadieSettings.ThumbnailImageSize.Height);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return File(result.Data.Bytes,
                result.ContentType,
                $"{result.Data.Caption ?? id.ToString()}.jpg",
                result.LastModified,
                result.ETag);
        }

        [HttpGet("artist-secondary/{id}/{imageId}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ArtistSecondaryImage(Guid id, int imageId, int? width, int? height)
        {
            var result = await ImageService.ArtistSecondaryImage(id, imageId, width, height);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return File(result.Data.Bytes,
                result.ContentType,
                $"{result.Data.Caption ?? id.ToString()}.jpg",
                result.LastModified,
                result.ETag);
        }

        [HttpGet("collection/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CollectionImage(Guid id, int? width, int? height)
        {
            var result = await ImageService.CollectionImage(id, width ?? RoadieSettings.ThumbnailImageSize.Width,
                height ?? RoadieSettings.ThumbnailImageSize.Height);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return File(result.Data.Bytes,
                result.ContentType,
                $"{result.Data.Caption ?? id.ToString()}.jpg",
                result.LastModified,
                result.ETag);
        }

        [HttpPost("delete/{id}")]
        [Authorize(Policy = "Editor")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await ImageService.Delete(await UserManager.GetUserAsync(User), id);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpGet("{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, int? width, int? height)
        {
            var result = await ImageService.ById(id, width, height);
            if (result == null) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return File(result.Data.Bytes,
                result.ContentType,
                $"{result.Data.Caption ?? id.ToString()}.jpg",
                result.LastModified,
                result.ETag);
        }

        [HttpGet("label/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> LabelImage(Guid id, int? width, int? height)
        {
            var result = await ImageService.LabelImage(id, width ?? RoadieSettings.ThumbnailImageSize.Width,
                height ?? RoadieSettings.ThumbnailImageSize.Height);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return File(result.Data.Bytes,
                result.ContentType,
                $"{result.Data.Caption ?? id.ToString()}.jpg",
                result.LastModified,
                result.ETag);
        }

        [HttpGet("playlist/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PlaylistImage(Guid id, int? width, int? height)
        {
            var result = await ImageService.PlaylistImage(id, width ?? RoadieSettings.ThumbnailImageSize.Width,
                height ?? RoadieSettings.ThumbnailImageSize.Height);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return File(result.Data.Bytes,
                result.ContentType,
                $"{result.Data.Caption ?? id.ToString()}.jpg",
                result.LastModified,
                result.ETag);
        }

        [HttpGet("release/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReleaseImage(Guid id, int? width, int? height)
        {
            var result = await ImageService.ReleaseImage(id, width ?? RoadieSettings.ThumbnailImageSize.Width,
                height ?? RoadieSettings.ThumbnailImageSize.Height);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return File(result.Data.Bytes,
                result.ContentType,
                $"{result.Data.Caption ?? id.ToString()}.jpg",
                result.LastModified,
                result.ETag);
        }

        [HttpGet("release-secondary/{id}/{imageId}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReleaseSecondaryImage(Guid id, int imageId, int? width, int? height)
        {
            var result = await ImageService.ReleaseSecondaryImage(id, imageId,
                width ?? RoadieSettings.MaximumImageSize.Width, height ?? RoadieSettings.MaximumImageSize.Height);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return File(result.Data.Bytes,
                result.ContentType,
                $"{result.Data.Caption ?? id.ToString()}.jpg",
                result.LastModified,
                result.ETag);
        }

        [HttpPost("search/artist/{query}/{resultsCount:int?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SearchForArtistImage(string query, int? resultsCount)
        {
            var result = await ImageService.Search(query, resultsCount ?? 10);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpPost("search/label/{query}/{resultsCount:int?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SearchForLabelImage(string query, int? resultsCount)
        {
            var result = await ImageService.Search(query, resultsCount ?? 10);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpPost("search/release/{query}/{resultsCount:int?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SearchForReleaseCover(string query, int? resultsCount)
        {
            var result = await ImageService.Search(query, resultsCount ?? 10);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpGet("track/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> TrackImage(Guid id, int? width, int? height)
        {
            var result = await ImageService.TrackImage(id, width ?? RoadieSettings.ThumbnailImageSize.Width,
                height ?? RoadieSettings.ThumbnailImageSize.Height);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return File(result.Data.Bytes,
                result.ContentType,
                $"{result.Data.Caption ?? id.ToString()}.jpg",
                result.LastModified,
                result.ETag);
        }

        /// <summary>
        ///     NOTE that user images/avatars are PNG not JPG this is so it looks better in the menus/applications
        /// </summary>
        [HttpGet("user/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UserImage(Guid id, int? width, int? height)
        {
            var result = await ImageService.UserImage(id, width ?? RoadieSettings.ThumbnailImageSize.Width,
                height ?? RoadieSettings.ThumbnailImageSize.Height);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return File(result.Data.Bytes,
                result.ContentType,
                $"{result.Data.Caption ?? id.ToString()}.png",
                result.LastModified,
                result.ETag);
        }
    }
}