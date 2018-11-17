using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Identity;
using System;
using System.IO;
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

        private ITrackService TrackService { get; }

        public PlayController(ITrackService trackService, IPlayActivityService playActivityService, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration, UserManager<ApplicationUser> userManager)
                            : base(cacheManager, configuration, userManager)
        {
            this._logger = logger.CreateLogger("RoadieApi.Controllers.PlayController");
            this.TrackService = trackService;
            this.PlayActivityService = playActivityService;
        }

        [HttpGet("track/{id}")]
        public async Task<FileStreamResult> StreamTrack(Guid id)
        {
            var user = await this.CurrentUserModel();
            var track = await this.TrackService.ById(user, id, null);
            if (track == null || track.IsNotFoundResult)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            var info = await this.TrackService.TrackStreamInfo(id,
                                                               Services.TrackService.DetermineByteStartFromHeaders(this.Request.Headers),
                                                               Services.TrackService.DetermineByteEndFromHeaders(this.Request.Headers, track.Data.FileSize));
            if (!info.IsSuccess)
            {
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            Response.Headers.Add("Content-Disposition", info.Data.ContentDisposition);
            Response.Headers.Add("X-Content-Duration", info.Data.ContentDuration);
            if (!info.Data.IsFullRequest)
            {
                Response.Headers.Add("Accept-Ranges", info.Data.AcceptRanges);
                Response.Headers.Add("Content-Range", info.Data.ContentRange);
            }
            Response.Headers.Add("Content-Length", info.Data.ContentLength);
            Response.ContentType = info.Data.ContentType;
            Response.StatusCode = info.Data.IsFullRequest ? (int)HttpStatusCode.OK : (int)HttpStatusCode.PartialContent;
            Response.Headers.Add("Last-Modified", info.Data.LastModified);
            Response.Headers.Add("ETag", info.Data.Etag);
            Response.Headers.Add("Cache-Control", info.Data.CacheControl);
            Response.Headers.Add("Expires", info.Data.Expires);
            var stream = new MemoryStream(info.Data.Bytes);
            var playListUser = await this.PlayActivityService.CreatePlayActivity(user, info.Data);
            this._logger.LogInformation($"StreamTrack PlayActivity `{ playListUser }`, StreamInfo `{ info.Data.ToString() }`");
            return new FileStreamResult(stream, info.Data.ContentType)
            {
                FileDownloadName = info.Data.FileName
            };
        }
    }
}