using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Identity;
using System.Threading.Tasks;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("stats")]
    [ApiController]
    [Authorize]
    public class StatsController : EntityControllerBase
    {
        private IStatisticsService StatisticsService { get; }

        public StatsController(IStatisticsService statisticsService, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration, UserManager<ApplicationUser> userManager)
            : base(cacheManager, configuration, userManager)
        {
            this._logger = logger.CreateLogger("RoadieApi.Controllers.StatsController");
            this.StatisticsService = statisticsService;
        }

        [HttpGet("library")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Library()
        {
            return Ok(await this.StatisticsService.LibraryStatistics());
        }
    }
}