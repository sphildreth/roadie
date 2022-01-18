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
using System.Net;
using System.Threading.Tasks;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("bookmarks")]
    [ApiController]
    [Authorize]
    public class BookmarkController : EntityControllerBase
    {
        private IBookmarkService BookmarkService { get; }

        public BookmarkController(
            IBookmarkService bookmarkService,
            ILogger<BookmarkController> logger,
            ICacheManager cacheManager,
            UserManager<User> userManager,
            IRoadieSettings roadieSettings)
            : base(cacheManager, roadieSettings, userManager)
        {
            Logger = logger;
            BookmarkService = bookmarkService;
        }

        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> List([FromQuery] PagedRequest request)
        {
            try
            {
                var result = await BookmarkService.ListAsync(await CurrentUserModel().ConfigureAwait(false), request).ConfigureAwait(false);
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
    }
}
