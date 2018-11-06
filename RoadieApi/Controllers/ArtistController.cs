using Mapster;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Data;
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
        public ArtistController(IArtistService artistService, IRoadieDbContext RoadieDbContext, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration)
            : base(RoadieDbContext, cacheManager, configuration)
        {
            this._logger = logger.CreateLogger("RoadieApi.Controllers.ArtistController");
            this._artistService = artistService;
        }

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(this._RoadieDbContext.Artists.ProjectToType<models.Artist>());
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id)
        {
            //var key = id.ToString();
            //var result = this._cacheManager.Get<models.Artist>(key, () =>
            //{
            //    var d = this._RoadieDbContext.Artists
            //                                 .Include(x => x.AssociatedArtists).Include("AssociatedArtists.AssociatedArtist")
            //                                 .FirstOrDefault(x => x.RoadieId == id);
            //    if (d != null)
            //    {
            //        //   var info = d.AssociatedArtists.Adapt<models.AssociatedArtistInfo>();
            //        return d.Adapt<models.Artist>();
            //    }
            //    return null;
            //}, key);
            //if (result == null)
            //{
            //    return NotFound();
            //}
            //return Ok(result);

            var key = id.ToString();
            var result = await this.ArtistService.ArtistById(null, id, new List<string> { "stats", "images", "associatedartists", "collections", "playlists", "contributions", "labels", "releases" });
            return Ok(result);
        }
    }
}