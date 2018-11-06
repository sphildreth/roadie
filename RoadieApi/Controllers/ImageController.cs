using Mapster;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Data;
using System;
using System.Linq;
using models = Roadie.Library.Models;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("image")]
    [ApiController]
    [Authorize]
    public class ImageController : EntityControllerBase
    {
        public ImageController(IRoadieDbContext RoadieDbContext, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration)
            : base(RoadieDbContext, cacheManager, configuration)
        {
            this._logger = logger.CreateLogger("RoadieApi.Controllers.ImageController"); ;
        }

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(this._RoadieDbContext.Tracks.ProjectToType<models.Image>());
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public IActionResult Get(Guid id)
        {
            var key = id.ToString();
            var result = this._cacheManager.Get<models.Image>(key, () =>
            {
                var d = this._RoadieDbContext.Images.FirstOrDefault(x => x.RoadieId == id);
                if (d != null)
                {
                    return d.Adapt<models.Image>();
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