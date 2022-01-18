using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Identity;
using Roadie.Library.Models.Pagination;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using models = Roadie.Library.Models;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("artists")]
    [ApiController]
    [Authorize]
    public class ArtistController : EntityControllerBase
    {
        private IArtistService ArtistService { get; }

        public ArtistController(
            IArtistService artistService,
            ILogger<ArtistController> logger,
            ICacheManager cacheManager,
            UserManager<User> userManager,
            IRoadieSettings roadieSettings)
            : base(cacheManager, roadieSettings, userManager)
        {
            Logger = logger;
            ArtistService = artistService;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, string inc = null)
        {
            var user = await CurrentUserModel().ConfigureAwait(false);
            var result =
                await ArtistService.ByIdAsync(user, id, (inc ?? models.Artist.DefaultIncludes).ToLower().Split(",")).ConfigureAwait(false);
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

        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> List([FromQuery] PagedRequest request, bool? doRandomize = false)
        {
            try
            {
                PagedResult<models.ArtistList> result = await ArtistService.ListAsync(await CurrentUserModel().ConfigureAwait(false),
                    request,
                    doRandomize ?? false,
                    false).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode((int)HttpStatusCode.Unauthorized);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("mergeArtists/{artistToMergeId}/{artistToMergeIntoId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [Authorize(Policy = "Editor")]
        public async Task<IActionResult> MergeArtists(Guid artistToMergeId, Guid artistToMergeIntoId)
        {
            var result = await ArtistService.MergeArtistsAsync(await UserManager.GetUserAsync(User).ConfigureAwait(false), artistToMergeId, artistToMergeIntoId).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
            {
                return NotFound();
            }

            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("setImageByUrl/{id}/{imageUrl}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [Authorize(Policy = "Editor")]
        public async Task<IActionResult> SetArtistImageByUrl(Guid id, string imageUrl)
        {
            var result =
                await ArtistService.SetReleaseImageByUrlAsync(await UserManager.GetUserAsync(User).ConfigureAwait(false), id, HttpUtility.UrlDecode(imageUrl)).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
            {
                return NotFound();
            }

            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("edit")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [Authorize(Policy = "Editor")]
        public async Task<IActionResult> Update(models.Artist artist)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await ArtistService.UpdateArtistAsync(await UserManager.GetUserAsync(User).ConfigureAwait(false), artist).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("uploadImage/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [Authorize(Policy = "Editor")]
        public async Task<IActionResult> UploadImage(Guid id, IFormFile file)
        {
            var result = await ArtistService.UploadArtistImageAsync(await UserManager.GetUserAsync(User).ConfigureAwait(false), id, file).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
            {
                return NotFound();
            }

            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }
    }
}
