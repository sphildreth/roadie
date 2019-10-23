using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public AdminController(IAdminService adminService, ILogger<AdminController> logger, ICacheManager cacheManager,
                    UserManager<ApplicationUser> userManager, IRoadieSettings roadieSettings)
            : base(cacheManager, roadieSettings, userManager)
        {
            Logger = logger;
            AdminService = adminService;
        }

        [HttpGet("clearcache")]
        [ProducesResponseType(200)]
        public IActionResult ClearCache()
        {
            CacheManager.Clear();
            Logger.LogWarning("Cache Cleared");
            return Ok();
        }

        [HttpPost("delete/artist/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteArtist(Guid id)
        {
            var result = await AdminService.DeleteArtist(await UserManager.GetUserAsync(User), id);
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

        [HttpPost("delete/artist/releases/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteArtistReleases(Guid id)
        {
            var result = await AdminService.DeleteArtistReleases(await UserManager.GetUserAsync(User), id);
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

        [HttpPost("delete/artistsecondaryimage/{id}/{index}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteArtistSecondaryImage(Guid id, int index)
        {
            var result = await AdminService.DeleteArtistSecondaryImage(await UserManager.GetUserAsync(User), id, index);
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

        [HttpPost("delete/release/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteRelease(Guid id, bool? doDeleteFiles)
        {
            var result = await AdminService.DeleteRelease(await UserManager.GetUserAsync(User), id, doDeleteFiles);
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

        [HttpPost("delete/label/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteLabel(Guid id)
        {
            var result = await AdminService.DeleteLabel(await UserManager.GetUserAsync(User), id);
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

        [HttpPost("delete/genre/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteGenre(Guid id)
        {
            var result = await AdminService.DeleteGenre(await UserManager.GetUserAsync(User), id);
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

        [HttpPost("delete/releasesecondaryimage/{id}/{index}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteReleaseSecondaryImage(Guid id, int index)
        {
            var result =
                await AdminService.DeleteReleaseSecondaryImage(await UserManager.GetUserAsync(User), id, index);
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpPost("delete/tracks")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteTracks([FromBody]IEnumerable<Guid> ids, bool? doDeleteFile)
        {
            var result = await AdminService.DeleteTracks(await UserManager.GetUserAsync(User), ids, doDeleteFile);
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


        [HttpPost("delete/track/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteTrack(Guid id, bool? doDeleteFile)
        {
            var result = await AdminService.DeleteTracks(await UserManager.GetUserAsync(User), new Guid[1] { id }, doDeleteFile);
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

        [HttpPost("delete/user/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var result = await AdminService.DeleteUser(await UserManager.GetUserAsync(User), id);
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

        [HttpPost("missingcollectionreleases")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> MissingCollectionReleases()
        {
            var result = await AdminService.MissingCollectionReleases(await UserManager.GetUserAsync(User));
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

        [HttpPost("scan/collection/rescanall")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ScanAllCollections()
        {
            var result = await AdminService.ScanAllCollections(await UserManager.GetUserAsync(User));
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

        [HttpPost("scan/artist/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ScanArtist(Guid id)
        {
            var result = await AdminService.ScanArtist(await UserManager.GetUserAsync(User), id);
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

        [HttpPost("scan/artists")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ScanArtists(IEnumerable<Guid> ids)
        {
            var result = await AdminService.ScanArtists(await UserManager.GetUserAsync(User), ids);
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


        [HttpPost("scan/collection/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ScanCollection(Guid id)
        {
            var result = await AdminService.ScanCollection(await UserManager.GetUserAsync(User), id);
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

        [HttpGet("scan/inbound")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ScanInbound()
        {
            var result = await AdminService.ScanInboundFolder(await UserManager.GetUserAsync(User));
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

        [HttpGet("scan/library")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ScanLibrary()
        {
            var result = await AdminService.ScanLibraryFolder(await UserManager.GetUserAsync(User));
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

        [HttpPost("scan/release/{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ScanRelease(Guid id)
        {
            var result = await AdminService.ScanRelease(await UserManager.GetUserAsync(User), id);
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

        [HttpPost("scan/releases")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ScanReleases(IEnumerable<Guid> ids)
        {
            var result = await AdminService.ScanReleases(await UserManager.GetUserAsync(User), ids);
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
    }
}