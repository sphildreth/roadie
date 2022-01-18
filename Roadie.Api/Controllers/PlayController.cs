using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Identity;
using Roadie.Library.Scrobble;
using Roadie.Library.Utility;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Roadie.Api.Controllers
{
    [Produces("application/json")]
    [Route("play")]
    [ApiController]
    [Authorize]
    public class PlayController : EntityControllerBase
    {
        private IPlayActivityService PlayActivityService { get; }

        private IReleaseService ReleaseService { get; }

        private ITrackService TrackService { get; }

        public PlayController(
            ITrackService trackService,
            IReleaseService releaseService,
            IPlayActivityService playActivityService,
            ILogger<PlayController> logger,
            ICacheManager cacheManager,
            UserManager<User> userManager,
            IRoadieSettings roadieSettings)
            : base(cacheManager, roadieSettings, userManager)
        {
            Logger = logger;
            TrackService = trackService;
            PlayActivityService = playActivityService;
            ReleaseService = releaseService;
        }

        [HttpGet("release/m3u/{id}.{m3u?}")]
        public async Task<FileResult> M3uForRelease(Guid id)
        {
            var user = await CurrentUserModel().ConfigureAwait(false);
            var release = await ReleaseService.ByIdAsync(user, id, new string[1] { "tracks" }).ConfigureAwait(false);
            if (release?.IsNotFoundResult != false)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            var m3u = M3uHelper.M3uContentForTracks(release.Data.Medias.SelectMany(x => x.Tracks));
            return File(Encoding.Default.GetBytes(m3u), "audio/mpeg-url");
        }

        [HttpGet("track/m3u/{id}.{m3u?}")]
        public async Task<FileResult> M3uForTrack(Guid id)
        {
            var user = await CurrentUserModel().ConfigureAwait(false);
            var track = await TrackService.ByIdAsyncAsync(user, id, null).ConfigureAwait(false);
            if (track?.IsNotFoundResult != false)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            var release = await ReleaseService.ByIdAsync(user, track.Data.Release.Id, new string[1] { "tracks" }).ConfigureAwait(false);
            if (release?.IsNotFoundResult != false)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            var m3u = M3uHelper.M3uContentForTracks(
               release.Data.Medias.SelectMany(x => x.Tracks).Where(x => x.Id == id));
            return File(Encoding.Default.GetBytes(m3u), "audio/mpeg-url");
        }

        /// <summary>
        ///     A scrobble is done at the end of a Track being played and is not exactly the same as a track activity (user can
        ///     fetch a track and not play it in its entirety).
        /// </summary>
        [HttpPost("track/scrobble/{id}/{startedPlaying}/{isRandom}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Scrobble(Guid id, string startedPlaying, bool isRandom)
        {
            var result = await PlayActivityService.ScrobbleAsync(await CurrentUserModel().ConfigureAwait(false), new ScrobbleInfo
            {
                TrackId = id,
                TimePlayed = SafeParser.ToDateTime(startedPlaying) ?? DateTime.UtcNow,
                IsRandomizedScrobble = isRandom
            }).ConfigureAwait(false);
            if (result?.IsNotFoundResult != false)
            {
                return NotFound();
            }

            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            return Ok(result);
        }

        /// <summary>
        ///     This was done to use a URL based token as many clients don't support adding Auth Bearer tokens to audio requests.
        /// </summary>
        [HttpGet("track/{userId}/{trackPlayToken}/{id}.{mp3?}")]
        [AllowAnonymous]
        public async Task<IActionResult> StreamTrack(int userId, string trackPlayToken, Guid id)
        {
            var user = UserManager.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null)
            {
                return StatusCode((int)HttpStatusCode.Unauthorized);
            }

            if (!ServiceBase.ConfirmTrackPlayToken(user, id, trackPlayToken))
            {
                return StatusCode((int)HttpStatusCode.Unauthorized);
            }
            return await StreamTrack(id, TrackService, PlayActivityService, UserModelForUser(user)).ConfigureAwait(false);
        }
    }
}
