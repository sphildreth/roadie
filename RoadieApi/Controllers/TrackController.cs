using Mapster;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using roadie.Library.Setttings;
using Roadie.Library.Caching;
using Roadie.Library.Data;
using System;
using System.Linq;
using models = Roadie.Api.Data.Models;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("track")]
    [ApiController]
    [Authorize]
    public class TrackController : EntityControllerBase
    {
        public TrackController(IRoadieDbContext roadieDbContext, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration, IRoadieSettings roadieSettings)
            : base(roadieDbContext, cacheManager, configuration, roadieSettings)
        {
            this._logger = logger.CreateLogger("RoadieApi.Controllers.TrackController"); ;
        }

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(this._roadieDbContext.Tracks.ProjectToType<models.Track>());
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public IActionResult Get(Guid id)
        {
            var key = id.ToString();
            var result = this._cacheManager.Get<models.Track>(key, () =>
            {
                var d = this._roadieDbContext.Tracks.FirstOrDefault(x => x.RoadieId == id);
                if (d != null)
                {
                    return d.Adapt<models.Track>();
                }
                return null;
            }, key);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
    }
}