using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Data;
using Roadie.Library.Identity;
using Roadie.Library.Models.Pagination;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using models = Roadie.Library.Models;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("releases")]
    [ApiController]
    [Authorize]
    public class ReleaseController : EntityControllerBase
    {
        private IReleaseService ReleaseService { get; }

        public ReleaseController(IReleaseService releaseService, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration, UserManager<ApplicationUser> userManager)
            : base(cacheManager, configuration, userManager)
        {
            this.Logger = logger.CreateLogger("RoadieApi.Controllers.ReleaseController"); ;
            this.ReleaseService = releaseService;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, string inc = null)
        {
            var result = await this.ReleaseService.ById(await this.CurrentUserModel(), id, (inc ?? models.Releases.Release.DefaultIncludes).ToLower().Split(","));
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

        [HttpPost("mergeReleases/{releaseToMergeId}/{releaseToMergeIntoId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [Authorize(Policy = "Editor")]
        public async Task<IActionResult> MergeReleases(Guid releaseToMergeId, Guid releaseToMergeIntoId, bool addAsMedia)
        {
            var result = await this.ReleaseService.MergeReleases(await this.CurrentUserModel(), releaseToMergeId, releaseToMergeIntoId, addAsMedia);
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

        [HttpPost("edit")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [Authorize(Policy="Editor")]
        public async Task<IActionResult> Update(models.Releases.Release release)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await this.ReleaseService.UpdateRelease(await this.CurrentUserModel(), release);
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

        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> List([FromQuery]PagedRequest request, string inc, bool? doRandomize = false)
        {
            try
            {
                var result = await this.ReleaseService.List(user: await this.CurrentUserModel(),
                                                           request: request,
                                                           doRandomize: doRandomize ?? false,
                                                           includes: (inc ?? models.Releases.Release.DefaultListIncludes).ToLower().Split(","));
                if (!result.IsSuccess)
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex);                
            }
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("setImageByUrl/{id}/{imageUrl}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [Authorize(Policy = "Editor")]
        public async Task<IActionResult> SetReleaseImageByUrl(Guid id, string imageUrl)
        {
            var result = await this.ReleaseService.SetReleaseImageByUrl(await this.CurrentUserModel(), id, HttpUtility.UrlDecode(imageUrl));
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

        [HttpPost("uploadImage/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [Authorize(Policy = "Editor")]
        public async Task<IActionResult> UploadImage(Guid id, IFormFile file)
        {
            var result = await this.ReleaseService.UploadReleaseImage(await this.CurrentUserModel(), id, file);
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
    }
}