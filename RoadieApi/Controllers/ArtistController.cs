using Mapster;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Library.Setttings;
using Roadie.Library.Caching;
using Roadie.Library.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using models = Roadie.Api.Data.Models;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("artist")]
    [ApiController]
    [Authorize]
    public class ArtistController : EntityControllerBase
    {
        public ArtistController(IRoadieDbContext RoadieDbContext, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration, IRoadieSettings RoadieSettings)
            : base(RoadieDbContext, cacheManager, configuration, RoadieSettings)
        {
            this._logger = logger.CreateLogger("RoadieApi.Controllers.ArtistController"); ;
        }

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(this._RoadieDbContext.Artists.ProjectToType<models.Artist>());
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public IActionResult Get(Guid id)
        {
            var key = id.ToString();
            var result = this._cacheManager.Get<models.Artist>(key, () =>
            {
                var d = this._RoadieDbContext.Artists.FirstOrDefault(x => x.RoadieId == id);
                if (d != null)
                {
                    return d.Adapt<models.Artist>();
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