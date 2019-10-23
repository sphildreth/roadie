using Microsoft.AspNetCore.Authorization;
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
using models = Roadie.Library.Models;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("tracks")]
    [ApiController]
    [Authorize]
    public class TrackController : EntityControllerBase
    {
        private ITrackService TrackService { get; }

        public TrackController(ITrackService trackService, ILogger<TrackController> logger, ICacheManager cacheManager,
                    UserManager<ApplicationUser> userManager, IRoadieSettings roadieSettings)
            : base(cacheManager, roadieSettings, userManager)
        {
            Logger = logger;
            TrackService = trackService;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, string inc = null)
        {
            var result = await TrackService.ById(await CurrentUserModel(), id,
                (inc ?? models.Track.DefaultIncludes).ToLower().Split(","));
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
                var result = await TrackService.List(request,
                    doRandomize: doRandomize,
                    roadieUser: await CurrentUserModel());
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

        [HttpPost("edit")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [Authorize(Policy = "Editor")]
        public async Task<IActionResult> Update(models.Track track)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await TrackService.UpdateTrack(await CurrentUserModel(), track);
            if (result == null || result.IsNotFoundResult) return NotFound();
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