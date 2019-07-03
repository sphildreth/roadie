using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Identity;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Playlists;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("playlists")]
    [ApiController]
    [Authorize]
    public class PlaylistController : EntityControllerBase
    {
        private IPlaylistService PlaylistService { get; }

        public PlaylistController(IPlaylistService playlistService, ILoggerFactory logger, ICacheManager cacheManager,
                    UserManager<ApplicationUser> userManager, IRoadieSettings roadieSettings)
            : base(cacheManager, roadieSettings, userManager)
        {
            Logger = logger.CreateLogger("RoadieApi.Controllers.PlaylistController");
            PlaylistService = playlistService;
        }

        [HttpPost("add")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddNewPlaylist([FromBody] Playlist model)
        {
            var result = await PlaylistService.AddNewPlaylist(await CurrentUserModel(), model);
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpPost("delete/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeletePlaylist(Guid id)
        {
            var result = await PlaylistService.DeletePlaylist(await CurrentUserModel(), id);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (result != null && result.IsAccessDeniedResult) return StatusCode((int)HttpStatusCode.Forbidden);
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, string inc = null)
        {
            var result = await PlaylistService.ById(await CurrentUserModel(), id,
                (inc ?? Playlist.DefaultIncludes).ToLower().Split(","));
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> List([FromQuery] PagedRequest request, string inc)
        {
            var result = await PlaylistService.List(roadieUser: await CurrentUserModel(),
                request: request);
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpPost("edit")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Playlist playlist)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await PlaylistService.UpdatePlaylist(await CurrentUserModel(), playlist);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }

        [HttpPost("edittracks")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateTracks(PlaylistTrackModifyRequest request)
        {
            var result = await PlaylistService.UpdatePlaylistTracks(await CurrentUserModel(), request);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess) return StatusCode((int)HttpStatusCode.InternalServerError);
            return Ok(result);
        }
    }
}