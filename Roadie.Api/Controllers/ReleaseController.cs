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
using Roadie.Library.Models.Releases;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("releases")]
    [ApiController]
    [Authorize]
    public class ReleaseController : EntityControllerBase
    {
        private IReleaseService ReleaseService { get; }

        public ReleaseController(IReleaseService releaseService, ILoggerFactory logger, ICacheManager cacheManager,
                    UserManager<ApplicationUser> userManager, IRoadieSettings roadieSettings)
            : base(cacheManager, roadieSettings, userManager)
        {
            Logger = logger.CreateLogger("RoadieApi.Controllers.ReleaseController");
            ;
            ReleaseService = releaseService;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, string inc = null)
        {
            var result = await ReleaseService.ById(await CurrentUserModel(), id,
                (inc ?? Release.DefaultIncludes).ToLower().Split(","));
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> List([FromQuery] PagedRequest request, string inc, bool? doRandomize = false)
        {
            try
            {
                var result = await ReleaseService.List(await CurrentUserModel(),
                    request,
                    doRandomize ?? false,
                    (inc ?? Release.DefaultListIncludes).ToLower().Split(","));
                if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
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

        [HttpPost("mergeReleases/{releaseToMergeId}/{releaseToMergeIntoId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [Authorize(Policy = "Editor")]
        public async Task<IActionResult> MergeReleases(Guid releaseToMergeId, Guid releaseToMergeIntoId, bool addAsMedia)
        {
            var result = await ReleaseService.MergeReleases(await UserManager.GetUserAsync(User), releaseToMergeId, releaseToMergeIntoId, addAsMedia);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpPost("setImageByUrl/{id}/{imageUrl}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [Authorize(Policy = "Editor")]
        public async Task<IActionResult> SetReleaseImageByUrl(Guid id, string imageUrl)
        {
            var result = await ReleaseService.SetReleaseImageByUrl(await UserManager.GetUserAsync(User), id, HttpUtility.UrlDecode(imageUrl));
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpPost("edit")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [Authorize(Policy = "Editor")]
        public async Task<IActionResult> Update(Release release)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await ReleaseService.UpdateRelease(await UserManager.GetUserAsync(User), release);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpPost("uploadImage/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [Authorize(Policy = "Editor")]
        public async Task<IActionResult> UploadImage(Guid id, IFormFile file)
        {
            var result = await ReleaseService.UploadReleaseImage(await UserManager.GetUserAsync(User), id, file);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }
    }
}