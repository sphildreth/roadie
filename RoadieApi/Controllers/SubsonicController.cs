using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Identity;
using Roadie.Library.Models.ThirdPartyApi.Subsonic;
using System.Net;
using System.Threading.Tasks;

namespace Roadie.Api.Controllers
{
    //[Produces("application/json")]
    [Route("subsonic/rest")]
    [ApiController]
    //[Authorize]
    public class SubsonicController : EntityControllerBase
    {
        private ISubsonicService SubsonicService { get; }
        private IPlayActivityService PlayActivityService { get; }
        private ITrackService TrackService { get; }

        public SubsonicController(ISubsonicService subsonicService, ITrackService trackService, IPlayActivityService playActivityService, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration, UserManager<ApplicationUser> userManager)
            : base(cacheManager, configuration, userManager)
        {
            this._logger = logger.CreateLogger("RoadieApi.Controllers.SubsonicController");
            this.SubsonicService = subsonicService;
            this.TrackService = trackService;
            this.PlayActivityService = playActivityService;
        }

        [HttpGet("getIndexes.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetIndexes([FromQuery]Request request, string musicFolderId = null, long? ifModifiedSince = null)
        {
            var result = await this.SubsonicService.GetIndexes(request, musicFolderId, ifModifiedSince);
            return this.BuildResponse(request, result.Data, "indexes");
        }

        [HttpGet("getMusicFolders.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetMusicFolders([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetMusicFolders(request);
            return this.BuildResponse(request, result.Data, "musicFolders");
        }

        [HttpGet("getMusicDirectory.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetMusicDirectory([FromQuery]Request request, string id)
        {
            var result = await this.SubsonicService.GetMusicDirectory(request, null, id);
            return this.BuildResponse(request, result.Data, "directory");
        }


        [HttpGet("getPlaylists.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetPlaylists([FromQuery]Request request, string username)
        {
            var result = await this.SubsonicService.GetPlaylists(request, null, username);
            return this.BuildResponse(request, result.Data, "playlists");
        }

        [HttpGet("getGenres.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetGenres([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetGenres(request);
            return this.BuildResponse(request, result.Data, "genres");
        }

        [HttpGet("getPodcasts.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetPodcasts([FromQuery]Request request, bool includeEpisodes)
        {
            var result = await this.SubsonicService.GetPodcasts(request);
            return this.BuildResponse(request, result.Data, "podcasts");
        }

        [HttpGet("getAlbumList.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAlbumList([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetAlbumList(request, null);
            return this.BuildResponse(request, result.Data, "albumList");
        }

        [HttpGet("stream.view")]
        [HttpPost("stream.view")]
        [ProducesResponseType(200)]
        public async Task<FileStreamResult> StreamTrack([FromQuery]Request request)
        {
            var trackId = request.TrackId;
            if (trackId == null)
            {
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            return await base.StreamTrack(trackId.Value, this.TrackService, this.PlayActivityService);
        }

        [HttpGet("getCoverArt.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetCoverArt([FromQuery]Request request, int? size)
        {
            var result = await this.SubsonicService.GetCoverArt(request, size);
            if (result == null || result.IsNotFoundResult)
            {
                return NotFound();
            }
            if (!result.IsSuccess)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return File(fileContents: result.Data.Bytes,
                        contentType: result.ContentType,
                        fileDownloadName: $"{ result.Data.Caption ??  request.id.ToString()}.jpg",
                        lastModified: result.LastModified,
                        entityTag: result.ETag);
        }


        [HttpGet("ping.view")]
        [HttpPost("ping.view")]
        [ProducesResponseType(200)]
        public IActionResult Ping([FromQuery]Request request)
        {
            var result = this.SubsonicService.Ping(request);
            return this.BuildResponse(request, result.Data);
        }




        #region Response Builder Methods

        private string BuildJsonResult(Response response, string responseType)
        {
            if (responseType == null)
            {
                return "{ \"subsonic-response\": { \"status\":\"" + response.status.ToString() + "\", \"version\": \"" + response.version + "\" }}";
            }
            return "{ \"subsonic-response\": { \"status\":\"" + response.status.ToString() + "\", \"version\": \"" + response.version + "\", \"" + responseType + "\":" + JsonConvert.SerializeObject(response.Item) + "}}";
        }

        private IActionResult BuildResponse(Request request, Response response = null, string reponseType = null)
        {
            if (request.IsJSONRequest)
            {
                this.Response.ContentType = "application/json";
                return Content(this.BuildJsonResult(response, reponseType));
            }
            return Ok(response);
        }

        #endregion Response Builder Methods
    }
}