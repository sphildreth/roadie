﻿using Microsoft.AspNetCore.Authorization;
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
using System.Linq;
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

        public ReleaseController(
            IReleaseService releaseService,
            ILogger<ReleaseController> logger,
            ICacheManager cacheManager,
            UserManager<User> userManager,
            IRoadieSettings roadieSettings)
            : base(cacheManager, roadieSettings, userManager)
        {
            Logger = logger;
            ReleaseService = releaseService;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, string inc = null)
        {
            var result = await ReleaseService.ByIdAsync(await CurrentUserModel().ConfigureAwait(false), id, (inc ?? Release.DefaultIncludes).ToLower().Split(",")).ConfigureAwait(false);
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
        public async Task<IActionResult> List([FromQuery] PagedRequest request, string inc, bool? doRandomize = false)
        {
            try
            {
                var result = await ReleaseService.ListAsync(await CurrentUserModel().ConfigureAwait(false),
                    request,
                    doRandomize ?? false,
                    (inc ?? Release.DefaultListIncludes).ToLower().Split(",")).ConfigureAwait(false);
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

        [HttpPost("mergeReleases/{releaseToMergeId}/{releaseToMergeIntoId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [Authorize(Policy = "Editor")]
        public async Task<IActionResult> MergeReleases(Guid releaseToMergeId, Guid releaseToMergeIntoId, bool addAsMedia)
        {
            var result = await ReleaseService.MergeReleasesAsync(await UserManager.GetUserAsync(User).ConfigureAwait(false), releaseToMergeId, releaseToMergeIntoId, addAsMedia).ConfigureAwait(false);
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
        public async Task<IActionResult> SetReleaseImageByUrl(Guid id, string imageUrl)
        {
            var result = await ReleaseService.SetReleaseImageByUrlAsync(await UserManager.GetUserAsync(User).ConfigureAwait(false), id, HttpUtility.UrlDecode(imageUrl)).ConfigureAwait(false);
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
        public async Task<IActionResult> Update(Release release)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await ReleaseService.UpdateReleaseAsync(await UserManager.GetUserAsync(User).ConfigureAwait(false), release).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
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
            var result = await ReleaseService.UploadReleaseImageAsync(await UserManager.GetUserAsync(User).ConfigureAwait(false), id, file).ConfigureAwait(false);
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
