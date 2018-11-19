using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Identity;
using Roadie.Library.Models.ThirdPartyApi.Subsonic;
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

        public SubsonicController(ISubsonicService subsonicService, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration, UserManager<ApplicationUser> userManager)
            : base(cacheManager, configuration, userManager)
        {
            this._logger = logger.CreateLogger("RoadieApi.Controllers.SubsonicController");
            this.SubsonicService = subsonicService;
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

        [HttpGet("getPlaylists.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetPlaylists([FromQuery]Request request, string username)
        {
            var result = await this.SubsonicService.GetPlaylists(request, null, username);
            return this.BuildResponse(request, result.Data, "playlists");
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