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
    [Route("artist")]
    [ApiController]
    [Authorize]
    public class ArtistController : EntityControllerBase
    {
        private IArtistService ArtistService { get; }

        public ArtistController(IArtistService artistService, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration, UserManager<ApplicationUser> userManager)
            : base(cacheManager, configuration, userManager)
        {
            this._logger = logger.CreateLogger("RoadieApi.Controllers.ArtistController");
            this.ArtistService = artistService;
        }

        //[EnableQuery]
        //public IActionResult Get()
        //{
        //    return Ok(this._RoadieDbContext.Artists.ProjectToType<models.Artist>());
        //}

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Artist>> Get(Guid id, string inc = null)
        {
            var result = await this.ArtistService.ById(await this.CurrentUserModel(), id, (inc ?? models.Artist.DefaultIncludes).ToLower().Split(","));
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

        [HttpPost]
        [ProducesResponseType(200)]
        public async Task<IActionResult> List(PagedRequest request)
        {
            var result = await this.ArtistService.List(roadieUser: await this.CurrentUserModel(),
                                                        request: request);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }
    }
}