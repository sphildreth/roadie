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
using System.Linq;
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

        public PlaylistController(
            IPlaylistService playlistService,
            ILogger<PlaylistController> logger,
            ICacheManager cacheManager,
            UserManager<User> userManager,
            IRoadieSettings roadieSettings)
            : base(cacheManager, roadieSettings, userManager)
        {
            Logger = logger;
            PlaylistService = playlistService;
        }

        [HttpPost("add")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddNewPlaylist([FromBody] Playlist model)
        {
            var result = await PlaylistService.AddNewPlaylistAsync(await CurrentUserModel().ConfigureAwait(false), model).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("delete/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeletePlaylist(Guid id)
        {
            var result = await PlaylistService.DeletePlaylistAsync(await CurrentUserModel().ConfigureAwait(false), id).ConfigureAwait(false);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, string inc = null)
        {
            var result = await PlaylistService.ByIdAsync(await CurrentUserModel().ConfigureAwait(false), id, (inc ?? Playlist.DefaultIncludes).ToLower().Split(",")).ConfigureAwait(false);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> List([FromQuery] PagedRequest request, string inc)
        {
            var result = await PlaylistService.ListAsync(roadieUser: await CurrentUserModel().ConfigureAwait(false), request: request).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            return Ok(result);
        }

        [HttpPost("edit")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Playlist playlist)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await PlaylistService.UpdatePlaylistAsync(await CurrentUserModel().ConfigureAwait(false), playlist).ConfigureAwait(false);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }

        [HttpPost("edittracks")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateTracks(PlaylistTrackModifyRequest request)
        {
            var result = await PlaylistService.UpdatePlaylistTracksAsync(await CurrentUserModel().ConfigureAwait(false), request).ConfigureAwait(false);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                if (result.IsAccessDeniedResult)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
                if (result.Messages?.Any() ?? false)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, result.Messages);
                }
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return Ok(result);
        }
    }
}
