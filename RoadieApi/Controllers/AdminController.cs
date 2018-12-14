using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Identity;
using Roadie.Library.Models.Pagination;
using System;
using System.Net;
using System.Threading.Tasks;
using models = Roadie.Library.Models;

namespace Roadie.Api.Controllers
{
    [Route("admin")]
    [ApiController]
    [Authorize("Editor")]
    public class AdminController : EntityControllerBase
    {
        private IAdminService AdminService { get; }

        public AdminController(IAdminService adminService, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration, UserManager<ApplicationUser> userManager)
            : base(cacheManager, configuration, userManager)
        {
            this.Logger = logger.CreateLogger("RoadieApi.Controllers.AdminController");
            this.AdminService = adminService;
        }

        [HttpGet("scan")]
        [ProducesResponseType(200)]
        [Authorize("Admin")]
        public async Task<IActionResult> Scan()
        {
            var result = await this.AdminService.ScanInboundFolder(await this.UserManager.GetUserAsync(User));
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }


        [HttpPost("{id}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ScanArtist(Guid id)
        {
            throw new NotImplementedException();
        }


    }
}