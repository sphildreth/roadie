using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("playactivities")]
    [ApiController]
    [Authorize]
    public class PlayActivityController : EntityControllerBase
    {
        private IPlayActivityService PlayActivityService { get; }

        public PlayActivityController(IPlayActivityService playActivityService, ILoggerFactory logger, ICacheManager cacheManager, 
                                      UserManager<ApplicationUser> userManager, IRoadieSettings roadieSettings)
            : base(cacheManager, roadieSettings, userManager)
        {
            this.Logger = logger.CreateLogger("RoadieApi.Controllers.PlayActivityController");
            this.PlayActivityService = playActivityService;
        }

        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> PlayActivity([FromQuery]PagedRequest request)
        {
            var result = await this.PlayActivityService.List(request);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpGet("{userId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> PlayActivity([FromQuery]PagedRequest request, Guid userId)
        {
            var user = this.UserManager.Users.FirstOrDefault(x => x.RoadieId == userId);
            if (user == null)
            {
                return NotFound();
            }
            var result = await this.PlayActivityService.List(request,
                                                            roadieUser: this.UserModelForUser(user));

            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }
    }
}