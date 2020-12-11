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
    [Route("system")]
    [ApiController]
    [Authorize]
    public class SystemController : EntityControllerBase
    {
        public SystemController(
            ILogger<StatsController> logger,
            ICacheManager cacheManager,
            UserManager<User> userManager,
            IRoadieSettings roadieSettings)
            : base(cacheManager, roadieSettings, userManager)
        {
            Logger = logger;
        }

        [HttpGet]
        [HttpPut]
        [AllowAnonymous]
        [Route("ping/{pong}")]
        public IActionResult Ping(string pong)
        {
            return Ok(pong);
        }
    }
}
