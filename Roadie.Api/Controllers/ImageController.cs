using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
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
    public class ImageController : EntityControllerBase
    {
        private IImageService ImageService { get; }

        public ImageController(
            IImageService imageService,
            ILogger<ImageController> logger,
            ICacheManager cacheManager,
            UserManager<User> userManager,
            IRoadieSettings roadieSettings)
            : base(cacheManager, roadieSettings, userManager)
        {
            Logger = logger;
            ImageService = imageService;
        }

        private IActionResult MakeFileResult(byte[] bytes, string fileName, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue eTag) => File(bytes, contentType, fileName, lastModified, eTag);

        [HttpGet("artist/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ArtistImage(Guid id, int? width, int? height)
        {
            var result = await ImageService.ArtistImageAsync(id, width ?? RoadieSettings.ThumbnailImageSize.Width, height ?? RoadieSettings.ThumbnailImageSize.Height).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return MakeFileResult(result.Data.Bytes, $"{id}.jpg", result.ContentType, result.LastModified, result.ETag);
        }

        [HttpGet("artist-secondary/{id}/{imageId}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ArtistSecondaryImage(Guid id, int imageId, int? width, int? height)
        {
            var result = await ImageService.ArtistSecondaryImageAsync(id, imageId, width, height).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return MakeFileResult(result.Data.Bytes, $"{id}.jpg", result.ContentType, result.LastModified, result.ETag);
        }

        [HttpGet("collection/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CollectionImage(Guid id, int? width, int? height)
        {
            var result = await ImageService.CollectionImageAsync(id, width ?? RoadieSettings.ThumbnailImageSize.Width, height ?? RoadieSettings.ThumbnailImageSize.Height).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return MakeFileResult(result.Data.Bytes, $"{id}.jpg", result.ContentType, result.LastModified, result.ETag);
        }

        [HttpGet("genre/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GenreImage(Guid id, int? width, int? height)
        {
            var result = await ImageService.GenreImageAsync(id, width ?? RoadieSettings.ThumbnailImageSize.Width, height ?? RoadieSettings.ThumbnailImageSize.Height).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return MakeFileResult(result.Data.Bytes, $"{id}.jpg", result.ContentType, result.LastModified, result.ETag);
        }

        [HttpGet("label/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> LabelImage(Guid id, int? width, int? height)
        {
            var result = await ImageService.LabelImageAsync(id, width ?? RoadieSettings.ThumbnailImageSize.Width, height ?? RoadieSettings.ThumbnailImageSize.Height).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return MakeFileResult(result.Data.Bytes, $"{id}.jpg", result.ContentType, result.LastModified, result.ETag);
        }

        [HttpGet("playlist/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PlaylistImage(Guid id, int? width, int? height)
        {
            var result = await ImageService.PlaylistImageAsync(id, width ?? RoadieSettings.ThumbnailImageSize.Width, height ?? RoadieSettings.ThumbnailImageSize.Height).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return MakeFileResult(result.Data.Bytes, $"{id}.jpg", result.ContentType, result.LastModified, result.ETag);
        }

        [HttpGet("release/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReleaseImage(Guid id, int? width, int? height)
        {
            var result = await ImageService.ReleaseImageAsync(id, width ?? RoadieSettings.ThumbnailImageSize.Width, height ?? RoadieSettings.ThumbnailImageSize.Height).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return MakeFileResult(result.Data.Bytes, $"{id}.jpg", result.ContentType, result.LastModified, result.ETag);
        }

        [HttpGet("release-secondary/{id}/{imageId}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReleaseSecondaryImage(Guid id, int imageId, int? width, int? height)
        {
            var result = await ImageService.ReleaseSecondaryImageAsync(id, imageId, width ?? RoadieSettings.MaximumImageSize.Width, height ?? RoadieSettings.MaximumImageSize.Height).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return MakeFileResult(result.Data.Bytes, $"{id}.jpg", result.ContentType, result.LastModified, result.ETag);
        }

        [HttpPost("search/artist/{query}/{resultsCount:int?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SearchForArtistImage(string query, int? resultsCount)
        {
            var result = await ImageService.SearchAsync(query, resultsCount ?? 10).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("search/genre/{query}/{resultsCount:int?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SearchForGenreImage(string query, int? resultsCount)
        {
            var result = await ImageService.SearchAsync(query, resultsCount ?? 10).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
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
            var result = await ImageService.SearchAsync(query, resultsCount ?? 10).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
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
            var result = await ImageService.SearchAsync(query, resultsCount ?? 10).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
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
            var result = await ImageService.TrackImageAsync(id, width ?? RoadieSettings.ThumbnailImageSize.Width, height ?? RoadieSettings.ThumbnailImageSize.Height).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return MakeFileResult(result.Data.Bytes, $"{id}.jpg", result.ContentType, result.LastModified, result.ETag);
        }

        /// <summary>
        /// NOTE that user images/avatars are GIF not JPG this is so it looks better in the menus/applications and allows for animated avatars.
        /// </summary>
        [HttpGet("user/{id}/{width:int?}/{height:int?}/{cacheBuster?}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UserImage(Guid id, int? width, int? height)
        {
            var result = await ImageService.UserImageAsync(id, width ?? RoadieSettings.ThumbnailImageSize.Width, height ?? RoadieSettings.ThumbnailImageSize.Height).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return MakeFileResult(result.Data.Bytes, $"{id}.gif", result.ContentType, result.LastModified, result.ETag);
        }
    }
}
