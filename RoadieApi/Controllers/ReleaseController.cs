using Microsoft.AspNetCore.Authorization;
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
using System.Net;
using System.Threading.Tasks;
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

        //[EnableQuery]
        //public IActionResult Get()
        //{
        //    return Ok(this._RoadieDbContext.Releases.ProjectToType<models.Releases.Release>());
        //}

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

        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> List([FromQuery]PagedRequest request, string inc)
        {
            var result = await this.ReleaseService.List(user: await this.CurrentUserModel(),
                                                               request: request);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("random")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> RandomList(PagedRequest request)
        {
            var result = await this.ReleaseService.List(user: await this.CurrentUserModel(),
                                                               request: request,
                                                               doRandomize: true);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }
    }
}