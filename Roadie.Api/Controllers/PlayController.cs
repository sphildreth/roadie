using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Identity;
using Roadie.Library.Utility;
using System;
using System.IO;
using System.Linq;
using System.Net;
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

        public PlayController(ITrackService trackService, IReleaseService releaseService, IPlayActivityService playActivityService, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration, UserManager<ApplicationUser> userManager)
                            : base(cacheManager, configuration, userManager)
        {
            this.Logger = logger.CreateLogger("RoadieApi.Controllers.PlayController");
            this.TrackService = trackService;
            this.PlayActivityService = playActivityService;
            this.ReleaseService = releaseService;
        }

        [HttpGet("release/m3u/{id}.{m3u?}")]
        public async Task<FileResult> M3uForRelease(Guid id)
        {
            var user = await this.CurrentUserModel();
            var release = await this.ReleaseService.ById(user, id, new string[1] { "tracks" });
            if (release == null || release.IsNotFoundResult)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            var m3u = M3uHelper.M3uContentForTracks(release.Data.Medias.SelectMany(x => x.Tracks));
            return File(System.Text.Encoding.Default.GetBytes(m3u), "audio/mpeg-url");
        }

        [HttpGet("track/m3u/{id}.{m3u?}")]
        public async Task<FileResult> M3uForTrack(Guid id)
        {
            var user = await this.CurrentUserModel();
            var track = await this.TrackService.ById(user, id, null);
            if (track == null || track.IsNotFoundResult)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            var release = await this.ReleaseService.ById(user, Guid.Parse(track.Data.Release.Value), new string[1] { "tracks" });
            if (release == null || release.IsNotFoundResult)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            var m3u = M3uHelper.M3uContentForTracks(release.Data.Medias.SelectMany(x => x.Tracks).Where(x => x.Id == id));
            return File(System.Text.Encoding.Default.GetBytes(m3u), "audio/mpeg-url");
        }

        /// <summary>
        /// This was done to use a URL based token as many clients don't support adding Auth Bearer tokens to audio requests.
        /// </summary>
        [HttpGet("track/{userId}/{trackPlayToken}/{id}.{mp3?}")]
        [AllowAnonymous]
        public async Task<IActionResult> StreamTrack(int userId, string trackPlayToken, Guid id)
        {
            var user = this.UserManager.Users.FirstOrDefault(x => x.Id == userId);
            if(user == null)
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }
            if(!ServiceBase.ConfirmTrackPlayToken(user, id, trackPlayToken))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }
            return await base.StreamTrack(id, this.TrackService, this.PlayActivityService, this.UserModelForUser(user));
        }
    }
}