using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    [Route("lookups")]
    [ApiController]
    [Authorize]
    public class LookupController : EntityControllerBase
    {
        private ILookupService LookupService { get; }

        public LookupController(ILabelService labelService, ILoggerFactory logger, ICacheManager cacheManager,
                    UserManager<ApplicationUser> userManager, ILookupService lookupService, IRoadieSettings roadieSettings)
            : base(cacheManager, roadieSettings, userManager)
        {
            Logger = logger.CreateLogger("RoadieApi.Controllers.LookupController");
            LookupService = lookupService;
        }

        [HttpGet("artistTypes")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ArtistTypes(Guid id, string inc = null)
        {
            var result = await LookupService.ArtistTypes();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpGet("bandStatus")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> BandStatus(Guid id, string inc = null)
        {
            var result = await LookupService.BandStatus();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpGet("bookmarkTypes")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> BookmarkTypes(Guid id, string inc = null)
        {
            var result = await LookupService.BookmarkTypes();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpGet("collectionTypes")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CollectionTypes(Guid id, string inc = null)
        {
            var result = await LookupService.CollectionTypes();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpGet("libraryStatus")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> LibraryStatus(Guid id, string inc = null)
        {
            var result = await LookupService.LibraryStatus();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpGet("queMessageTypes")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> QueMessageTypes(Guid id, string inc = null)
        {
            var result = await LookupService.QueMessageTypes();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpGet("releaseTypes")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReleaseTypes(Guid id, string inc = null)
        {
            var result = await LookupService.ReleaseTypes();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpGet("requestStatus")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RequestStatus(Guid id, string inc = null)
        {
            var result = await LookupService.RequestStatus();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpGet("status")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Status(Guid id, string inc = null)
        {
            var result = await LookupService.Status();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }
    }
}