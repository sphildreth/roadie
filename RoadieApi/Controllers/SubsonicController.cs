using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Identity;
using Roadie.Library.Models.ThirdPartyApi.Subsonic;
using System;
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
        private IPlayActivityService PlayActivityService { get; }
        private ISubsonicService SubsonicService { get; }
        private ITrackService TrackService { get; }

        public SubsonicController(ISubsonicService subsonicService, ITrackService trackService, IPlayActivityService playActivityService, ILoggerFactory logger, ICacheManager cacheManager, IConfiguration configuration, UserManager<ApplicationUser> userManager)
            : base(cacheManager, configuration, userManager)
        {
            this.Logger = logger.CreateLogger("RoadieApi.Controllers.SubsonicController");
            this.SubsonicService = subsonicService;
            this.TrackService = trackService;
            this.PlayActivityService = playActivityService;
        }

        [HttpGet("getAlbumList.view")]
        [HttpPost("getAlbumList.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAlbumList([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetAlbumList(request, null, AlbumListVersions.One);
            return this.BuildResponse(request, result.Data, "albumList");
        }

        [HttpGet("getAlbumList2.view")]
        [HttpPost("getAlbumList2.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAlbumList2([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetAlbumList(request, null, AlbumListVersions.Two);
            return this.BuildResponse(request, result.Data, "albumList");
        }

        [HttpGet("getArtistInfo.view")]
        [HttpPost("getArtistInfo.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetArtistInfo([FromQuery]Request request, string id, int? count, bool? includeNotPresent)
        {
            var result = await this.SubsonicService.GetArtistInfo(request, id, count, includeNotPresent ?? false, ArtistInfoVersion.One);
            return this.BuildResponse(request, result.Data, "artistInfo");
        }

        [HttpGet("getArtistInfo2.view")]
        [HttpPost("getArtistInfo2.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetArtistInfo2([FromQuery]Request request, string id, int? count, bool? includeNotPresent)
        {
            var result = await this.SubsonicService.GetArtistInfo(request, id, count, includeNotPresent ?? false, ArtistInfoVersion.Two);
            return this.BuildResponse(request, result.Data, "artistInfo2");
        }

        [HttpGet("getCoverArt.view")]
        [HttpPost("getCoverArt.view")]
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
                this.Logger.LogWarning($"GetCoverArt Failed For [{ JsonConvert.SerializeObject(request) }]");
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            return File(fileContents: result.Data.Bytes,
                        contentType: result.ContentType,
                        fileDownloadName: $"{ result.Data.Caption ?? request.id.ToString()}.jpg",
                        lastModified: result.LastModified,
                        entityTag: result.ETag);
        }

        [HttpGet("getGenres.view")]
        [HttpPost("getGenres.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetGenres([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetGenres(request);
            return this.BuildResponse(request, result.Data, "genres");
        }

        [HttpGet("getIndexes.view")]
        [HttpPost("getIndexes.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetIndexes([FromQuery]Request request, string musicFolderId = null, long? ifModifiedSince = null)
        {
            var result = await this.SubsonicService.GetIndexes(request, null, musicFolderId, ifModifiedSince);
            return this.BuildResponse(request, result.Data, "indexes");
        }

        [HttpGet("getMusicDirectory.view")]
        [HttpPost("getMusicDirectory.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetMusicDirectory([FromQuery]Request request, string id)
        {
            var result = await this.SubsonicService.GetMusicDirectory(request, null, id);
            return this.BuildResponse(request, result.Data, "directory");
        }

        [HttpGet("getMusicFolders.view")]
        [HttpPost("getMusicFolders.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetMusicFolders([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetMusicFolders(request);
            return this.BuildResponse(request, result.Data, "musicFolders");
        }

        [HttpGet("getPlaylists.view")]
        [HttpPost("getPlaylists.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetPlaylists([FromQuery]Request request, string username)
        {
            var result = await this.SubsonicService.GetPlaylists(request, null, username);
            return this.BuildResponse(request, result.Data, "playlists");
        }

        [HttpGet("getPodcasts.view")]
        [HttpPost("getPodcasts.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetPodcasts([FromQuery]Request request, bool includeEpisodes)
        {
            var result = await this.SubsonicService.GetPodcasts(request);
            return this.BuildResponse(request, result.Data, "podcasts");
        }

        [HttpGet("ping.view")]
        [HttpPost("ping.view")]
        [ProducesResponseType(200)]
        public IActionResult Ping([FromQuery]Request request)
        {
            if(request.IsJSONRequest)
            {
                var result = this.SubsonicService.Ping(request);
                return this.BuildResponse(request, result.Data);
            }
            return Content("<subsonic-response xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://subsonic.org/restapi\" status=\"ok\" version=\"1.16.0\" />", "application/xml");
        }

        [HttpGet("getLicense.view")]
        [HttpPost("getLicense.view")]
        [ProducesResponseType(200)]
        public IActionResult GetLicense([FromQuery]Request request)
        {
            var result = this.SubsonicService.GetLicense(request);
            return this.BuildResponse(request, result.Data, "license");
        }


        /// <summary>
        /// Returns albums, artists and songs matching the given search criteria. Supports paging through the result.
        /// </summary>
        [HttpGet("search2.view")]
        [HttpPost("search2.view")]
        [ProducesResponseType(200)]        
        public async Task<IActionResult> Search2([FromQuery]Request request)
        {
            var result = await this.SubsonicService.Search(request, null);
            return this.BuildResponse(request, result.Data, "searchResult2");
        }

        [HttpGet("getAlbum.view")]
        [HttpPost("getAlbum.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAlbum([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetAlbum(request, null);
            return this.BuildResponse(request, result.Data, "album");
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
            var acceptHeader = this.Request.Headers["Accept"];
            this.Logger.LogTrace($"Subsonic Request: Method [{ this.Request.Method }], Accept Header [{ acceptHeader }], Path [{ this.Request.Path }], Query String [{ this.Request.QueryString }], Request [{ JsonConvert.SerializeObject(request) }] ResponseType [{ reponseType }]");
            if (request.IsJSONRequest)
            {
                this.Response.ContentType = "application/json";
                return Content(this.BuildJsonResult(response, reponseType));
            }
            this.Response.ContentType = "application/xml";
            return Ok(response);
        }

        #endregion Response Builder Methods
    }
}