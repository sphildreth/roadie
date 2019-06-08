using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Identity;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using System;
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

        public TrackController(ITrackService trackService, ILoggerFactory logger, ICacheManager cacheManager, 
                               UserManager<ApplicationUser> userManager, IRoadieSettings roadieSettings)
            : base(cacheManager, roadieSettings, userManager)
        {
            this.Logger = logger.CreateLogger("RoadieApi.Controllers.TrackController");
            this.TrackService = trackService;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, string inc = null)
        {
            var result = await this.TrackService.ById(await this.CurrentUserModel(), id, (inc ?? models.Track.DefaultIncludes).ToLower().Split(","));
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
                var result = await this.TrackService.List(request: request,
                                                  doRandomize: doRandomize,
                                                  roadieUser: await this.CurrentUserModel());
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
                this.Logger.LogError(ex);
            }
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }

        [HttpPost("edit")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [Authorize(Policy = "Editor")]
        public async Task<IActionResult> Update(Track track)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await this.TrackService.UpdateTrack(await this.CurrentUserModel(), track);
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