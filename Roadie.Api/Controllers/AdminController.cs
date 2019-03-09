using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Identity;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Roadie.Api.Controllers
{
    [Route("admin")]
    [ApiController]
    [Authorize("Admin")]
    public class AdminController : EntityControllerBase
    {
        private IAdminService AdminService { get; }

        public AdminController(IAdminService adminService, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration, UserManager<ApplicationUser> userManager)
            : base(cacheManager, configuration, userManager)
        {
            this.Logger = logger.CreateLogger("RoadieApi.Controllers.AdminController");
            this.AdminService = adminService;
        }

        [HttpGet("clearcache")]
        [ProducesResponseType(200)]
        public IActionResult ClearCache()
        {
            this.CacheManager.Clear();
            this.Logger.LogInformation("Cache Cleared");
            return Ok();
        }


        [HttpGet("scan/inbound")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ScanInbound()
        {
            var result = await this.AdminService.ScanInboundFolder(await this.UserManager.GetUserAsync(User));
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpGet("scan/library")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ScanLibrary()
        {
            var result = await this.AdminService.ScanLibraryFolder(await this.UserManager.GetUserAsync(User));
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("scan/artist/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ScanArtist(Guid id)
        {
            var result = await this.AdminService.ScanArtist(await this.UserManager.GetUserAsync(User), id);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("scan/release/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ScanRelease(Guid id)
        {
            var result = await this.AdminService.ScanRelease(await this.UserManager.GetUserAsync(User), id);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("scan/collection/rescanall")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ScanAllCollections()
        {
            var result = await this.AdminService.ScanAllCollections(await this.UserManager.GetUserAsync(User));
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("scan/collection/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ScanCollection(Guid id)
        {
            var result = await this.AdminService.ScanCollection(await this.UserManager.GetUserAsync(User), id);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("missingcollectionreleases")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> MissingCollectionReleases()
        {
            var result = await this.AdminService.MissingCollectionReleases(await this.UserManager.GetUserAsync(User));
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }


        [HttpPost("delete/release/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteRelease(Guid id, bool? doDeleteFiles)
        {
            var result = await this.AdminService.DeleteRelease(await this.UserManager.GetUserAsync(User), id, doDeleteFiles);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("delete/track/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteTrack(Guid id, bool? doDeleteFile)
        {
            var result = await this.AdminService.DeleteTrack(await this.UserManager.GetUserAsync(User), id, doDeleteFile);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("delete/artist/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteArtist(Guid id)
        {
            var result = await this.AdminService.DeleteArtist(await this.UserManager.GetUserAsync(User), id);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("delete/artist/releases/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteArtistReleases(Guid id)
        {
            var result = await this.AdminService.DeleteArtistReleases(await this.UserManager.GetUserAsync(User), id);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }
    }
}