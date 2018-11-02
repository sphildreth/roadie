using Mapster;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    [Route("release")]
    [ApiController]
    [Authorize]
    public class ReleaseController : EntityControllerBase
    {
        public ReleaseController(IRoadieDbContext roadieDbContext, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration, IRoadieSettings roadieSettings)
            : base(roadieDbContext, cacheManager, configuration, roadieSettings)
        {
            this._logger = logger.CreateLogger("RoadieApi.Controllers.ReleaseController"); ;
        }

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(this._roadieDbContext.Releases.ProjectToType<models.Release>());
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public IActionResult Get(Guid id)
        {
            var key = id.ToString();
            var result = this._cacheManager.Get<models.Release>(key, () =>
            {
                var d = this._roadieDbContext
                            .Releases
                            .Include(x => x.Artist)
                            .Include(x => x.Labels).Include("Labels.Label")
                            .Include(x => x.Medias).Include("Medias.Tracks")
                            .Include(x => x.Genres).Include("Genres.Genre")
                            .Include(x => x.Collections).Include("Collections.Collection")
                            .FirstOrDefault(x => x.RoadieId == id);
                if (d != null)
                {
                    return d.Adapt<models.Release>();
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