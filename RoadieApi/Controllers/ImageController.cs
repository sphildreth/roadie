using Mapster;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Data;
using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using models = Roadie.Library.Models;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("image")]
    [ApiController]
    [Authorize]
    public class ImageController : EntityControllerBase
    {
        private IImageService ImageService { get; }

        public ImageController(IImageService imageService, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration)
            : base(cacheManager, configuration)
        {
            this._logger = logger.CreateLogger("RoadieApi.Controllers.ImageController"); ;
            this.ImageService = imageService;
        }

        //[EnableQuery]
        //public IActionResult Get()
        //{
        //    return Ok(this._RoadieDbContext.Tracks.ProjectToType<models.Image>());
        //}

        //[HttpGet("{id}")]
        //[ProducesResponseType(200)]
        //[ProducesResponseType(404)]
        //public IActionResult Get(Guid id)
        //{
        //    var key = id.ToString();
        //    var result = this._cacheManager.Get<models.Image>(key, () =>
        //    {
        //        var d = this._RoadieDbContext.Images.FirstOrDefault(x => x.RoadieId == id);
        //        if (d != null)
        //        {
        //            return d.Adapt<models.Image>();
        //        }
        //        return null;
        //    }, key);
        //    if (result == null)
        //    {
        //        return NotFound();
        //    }
        //    return Ok(result);
        //}


        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [Route("{id}/{width}/{height}")]
        public async Task<IActionResult> Get(Guid id, int? width, int? height)
        {
            var result = await this.ImageService.ImageById(id, width, height);
            if (result == null)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return File(fileContents:result.Data.Bytes, 
                        contentType: result.ContentType, 
                        fileDownloadName: result.Data.Caption ?? id.ToString(),
                        lastModified: result.LastModified, 
                        entityTag: result.ETag);
        }
    }
}