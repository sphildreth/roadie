﻿using Mapster;
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
using System.Threading.Tasks;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("playlist")]
    [ApiController]
    [Authorize]
    public class PlaylistController : EntityControllerBase
    {
        private IPlaylistService PlaylistService { get; }

        public PlaylistController(IPlaylistService playlistService, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration, UserManager<ApplicationUser> userManager)
            : base(cacheManager, configuration, userManager)
        {
            this._logger = logger.CreateLogger("RoadieApi.Controllers.TrackController");
            this.PlaylistService = playlistService;
        }

        //[EnableQuery]
        //public IActionResult Get()
        //{
        //    return Ok(this._RoadieDbContext.Tracks.ProjectToType<models.Track>());
        //}

        //[HttpGet("{id}")]
        //[ProducesResponseType(200)]
        //[ProducesResponseType(404)]
        //public IActionResult Get(Guid id)
        //{
        //    var key = id.ToString();
        //    var result = this._cacheManager.Get<models.Track>(key, () =>
        //    {
        //        var d = this._RoadieDbContext.Tracks.FirstOrDefault(x => x.RoadieId == id);
        //        if (d != null)
        //        {
        //            return d.Adapt<models.Track>();
        //        }
        //        return null;
        //    }, key);
        //    if (result == null)
        //    {
        //        return NotFound();
        //    }
        //    return Ok(result);
        //}

        [HttpPost("playactivity")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> PlayActivity(PagedRequest request)
        {
            var result = await this.PlaylistService.List(request);

            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("playactivity/{userId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> PlayActivity(PagedRequest request, Guid userId)
        {
            var user = await this.UserManager.FindByIdAsync(userId.ToString());
            var result = await this.PlaylistService.List(request, user.Adapt<Library.Models.Users.User>());

            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }
    }
}