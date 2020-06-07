using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Identity;
using System.Collections.Generic;
using System.Diagnostics;
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

        public StatsController(
            IStatisticsService statisticsService,
            ILogger<StatsController> logger,
            ICacheManager cacheManager,
            UserManager<User> userManager,
            IRoadieSettings roadieSettings)
            : base(cacheManager, roadieSettings, userManager)
        {
            Logger = logger;
            StatisticsService = statisticsService;
        }

        [HttpGet("artistsByDate")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ArtistsByDate() => Ok(await StatisticsService.ArtistsByDateAsync().ConfigureAwait(false));

        [HttpGet("info")]
        [ProducesResponseType(200)]
        [AllowAnonymous]
        public IActionResult Info()
        {
            var proc = Process.GetCurrentProcess();
            var mem = proc.WorkingSet64;
            var cpu = proc.TotalProcessorTime;
            var messages = new List<string>
            {
                "▜ Memory Information: ",
                $"My process used working set {mem / 1024.0:n3} K of working set and CPU {cpu.TotalMilliseconds:n} msec"
            };
            foreach (var aProc in Process.GetProcesses())
            {
                messages.Add(string.Format("Proc {0,30}  CPU {1,-20:n} msec", aProc.ProcessName,
                   cpu.TotalMilliseconds));
            }

            messages.Add("▟ Memory Information: ");

            return Ok(messages);
        }

        [HttpGet("library")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Library() => Ok(await StatisticsService.LibraryStatisticsAsync().ConfigureAwait(false));

        [HttpGet("ping")]
        [ProducesResponseType(200)]
        [AllowAnonymous]
        public IActionResult Ping() => Ok("pong");

        [HttpGet("releasesByDate")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ReleasesByDate() => Ok(await StatisticsService.ReleasesByDateAsync().ConfigureAwait(false));

        [HttpGet("releasesByDecade")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ReleasesByDecade() => Ok(await StatisticsService.ReleasesByDecadeAsync().ConfigureAwait(false));

        [HttpGet("songsPlayedByDate")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> SongsPlayedByDate() => Ok(await StatisticsService.SongsPlayedByDateAsync().ConfigureAwait(false));

        [HttpGet("songsPlayedByUser")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> SongsPlayedByUser() => Ok(await StatisticsService.SongsPlayedByUserAsync().ConfigureAwait(false));
    }
}