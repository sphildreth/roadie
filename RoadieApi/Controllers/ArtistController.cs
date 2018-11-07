using Mapster;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using models = Roadie.Library.Models;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("artist")]
    [ApiController]
    [Authorize]
    public class ArtistController : EntityControllerBase
    {
        private readonly IArtistService _artistService;
        private IArtistService ArtistService
        {
            get
            {
                return this._artistService;
            }
        }
        public ArtistController(IArtistService artistService, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration)
            : base(cacheManager, configuration)
        {
            this._logger = logger.CreateLogger("RoadieApi.Controllers.ArtistController");
            this._artistService = artistService;
        }

        //[EnableQuery]
        //public IActionResult Get()
        //{
        //    return Ok(this._RoadieDbContext.Artists.ProjectToType<models.Artist>());
        //}

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, string inc = null)
        {
            var key = id.ToString();
            var result = await this._cacheManager.GetAsync<Artist>(key, async () =>
            {
                var op = await this.ArtistService.ArtistById(null, id, (inc ?? "stats,imaes,associatedartists,collections,playlists,contributions,labels").ToLower().Split(","));
                return op.Data;
            }, key);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
    }
}