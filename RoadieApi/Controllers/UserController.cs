using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Identity;
using Roadie.Library.Models.Pagination;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("users")]
    [ApiController]
    [Authorize]
    public class UserController : EntityControllerBase
    {
        private IUserService UserService { get; }

        public UserController(IUserService userService, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration, UserManager<ApplicationUser> userManager)
            : base(cacheManager, configuration, userManager)
        {
            this.Logger = logger.CreateLogger("RoadieApi.Controllers.LabelController");
            this.UserService = userService;
        }

        //[EnableQuery]
        //public IActionResult Get()
        //{
        //    return Ok(this._RoadieDbContext.Labels.ProjectToType<models.Label>());
        //}

        //[HttpGet("{id}")]
        //[ProducesResponseType(200)]
        //[ProducesResponseType(404)]
        //public IActionResult Get(Guid id)
        //{
        //    var key = id.ToString();
        //    var result = this._cacheManager.Get<models.Label>(key, () =>
        //    {
        //        var d = this._RoadieDbContext.Labels.FirstOrDefault(x => x.RoadieId == id);
        //        if (d != null)
        //        {
        //            return d.Adapt<models.Label>();
        //        }
        //        return null;
        //    }, key);
        //    if (result == null)
        //    {
        //        return NotFound();
        //    }
        //    return Ok(result);
        //}


        [HttpPost("setArtistRating/{releaseId}/{rating}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> SetArtistRating(Guid releaseId, short rating)
        {
            var result = await this.UserService.SetArtistRating(releaseId, await this.CurrentUserModel(), rating);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            this.CacheManager.ClearRegion(EntityControllerBase.ControllerCacheRegionUrn);
            return Ok(result);
        }

        [HttpPost("setReleaseRating/{releaseId}/{rating}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> SetReleaseRating(Guid releaseId, short rating)
        {
            var result = await this.UserService.SetReleaseRating(releaseId, await this.CurrentUserModel(), rating);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            this.CacheManager.ClearRegion(EntityControllerBase.ControllerCacheRegionUrn);
            return Ok(result);
        }

        [HttpPost("setTrackRating/{releaseId}/{rating}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> SetTrackRating(Guid releaseId, short rating)
        {
            var result = await this.UserService.SetTrackRating(releaseId, await this.CurrentUserModel(), rating);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            this.CacheManager.ClearRegion(EntityControllerBase.ControllerCacheRegionUrn);
            return Ok(result);
        }

        [HttpPost("setArtistFavorite/{artistId}/{isFavorite}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> SetArtistFavorite(Guid artistId, bool isFavorite)
        {
            var result = await this.UserService.SetArtistFavorite(artistId, await this.CurrentUserModel(), isFavorite);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            this.CacheManager.ClearRegion(EntityControllerBase.ControllerCacheRegionUrn);
            return Ok(result);
        }

        [HttpPost("setReleaseFavorite/{releaseId}/{isFavorite}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> SetReleaseFavorite(Guid releaseId, bool isFavorite)
        {
            var result = await this.UserService.SetReleaseFavorite(releaseId, await this.CurrentUserModel(), isFavorite);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            this.CacheManager.ClearRegion(EntityControllerBase.ControllerCacheRegionUrn);
            return Ok(result);
        }

        [HttpPost("setArtistBookmark/{artistId}/{isBookmarked}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> SetArtistBookmark(Guid artistId, bool isBookmarked)
        {
            var result = await this.UserService.SetArtistBookmark(artistId, await this.CurrentUserModel(), isBookmarked);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            this.CacheManager.ClearRegion(EntityControllerBase.ControllerCacheRegionUrn);
            return Ok(result);
        }


        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> List([FromQuery]PagedRequest request)
        {
            var result = await this.UserService.List(request);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        public class PagingParams
        {
            public int Page { get; set; } = 1;
            public int Limit { get; set; } = 5;
        }
    }
}