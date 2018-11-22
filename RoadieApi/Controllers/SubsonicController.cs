using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Extensions;
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

        [HttpGet("getAlbum.view")]
        [HttpPost("getAlbum.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAlbum([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetAlbum(request, null);
            return this.BuildResponse(request, result, "album");
        }

        [HttpGet("getAlbum.view")]
        [HttpPost("getAlbum.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetSong([FromQuery]Request request, string id)
        {
            var result = await this.SubsonicService.GetAlbum(request, null);
            return this.BuildResponse(request, result, "song");
        }

        [HttpGet("getArtist.view")]
        [HttpPost("getArtist.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetArtist([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetArtist(request, null);
            return this.BuildResponse(request, result, "artist");
        }

        [HttpGet("getAlbumInfo.view")]
        [HttpPost("getAlbumInfo.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAlbumInfo([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetAlbumInfo(request, null, AlbumInfoVersion.One);
            return this.BuildResponse(request, result, "albumInfo");
        }

        [HttpGet("getAlbumInfo2.view")]
        [HttpPost("getAlbumInfo2.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAlbumInfo2([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetAlbumInfo(request, null, AlbumInfoVersion.Two);
            return this.BuildResponse(request, result, "albumInfo");
        }

        [HttpGet("getVideos.view")]
        [HttpPost("getVideos.view")]
        [ProducesResponseType(200)]
        public IActionResult GetVideos([FromQuery]Request request)
        {
            var result = this.SubsonicService.GetVideos(request);
            return this.BuildResponse(request, result, "videos");
        }

        [HttpGet("getLyrics.view")]
        [HttpPost("getLyrics.view")]
        [ProducesResponseType(200)]
        public IActionResult GetLyrics([FromQuery]Request request, string artist, string title)
        {
            var result = this.SubsonicService.GetLyrics(request, artist, title);
            return this.BuildResponse(request, result, "lyrics ");
        }

        [HttpGet("getAlbumList.view")]
        [HttpPost("getAlbumList.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAlbumList([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetAlbumList(request, null, AlbumListVersions.One);
            return this.BuildResponse(request, result, "albumList");
        }

        [HttpGet("getAlbumList2.view")]
        [HttpPost("getAlbumList2.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAlbumList2([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetAlbumList(request, null, AlbumListVersions.Two);
            return this.BuildResponse(request, result, "albumList");
        }

        [HttpGet("getArtistInfo.view")]
        [HttpPost("getArtistInfo.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetArtistInfo([FromQuery]Request request, int? count, bool? includeNotPresent)
        {
            var result = await this.SubsonicService.GetArtistInfo(request, count, includeNotPresent ?? false, ArtistInfoVersion.One);
            return this.BuildResponse(request, result, "artistInfo");
        }

        [HttpGet("getArtistInfo2.view")]
        [HttpPost("getArtistInfo2.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetArtistInfo2([FromQuery]Request request, int? count, bool? includeNotPresent)
        {
            var result = await this.SubsonicService.GetArtistInfo(request, count, includeNotPresent ?? false, ArtistInfoVersion.Two);
            return this.BuildResponse(request, result, "artistInfo2");
        }

        [HttpGet("getArtists.view")]
        [HttpPost("getArtists.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetArtists([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetArtists(request, null);
            return this.BuildResponse(request, result, "artists");
        }

        [HttpGet("getAvatar.view")]
        [HttpPost("getAvatar.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAvatar([FromQuery]Request request, string username)
        {
            //[HttpGet("user/{id}/{width:int?}/{height:int?}")]
            var user = await this.UserManager.FindByNameAsync(username);
            return Redirect($"/images/user/{ user.RoadieId }/{this.RoadieSettings.ThumbnailImageSize.Width}/{this.RoadieSettings.ThumbnailImageSize.Height}");
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
            return this.BuildResponse(request, result, "genres");
        }

        [HttpGet("getIndexes.view")]
        [HttpPost("getIndexes.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetIndexes([FromQuery]Request request, long? ifModifiedSince = null)
        {
            var result = await this.SubsonicService.GetIndexes(request, null, ifModifiedSince);
            return this.BuildResponse(request, result, "indexes");
        }

        [HttpGet("getLicense.view")]
        [HttpPost("getLicense.view")]
        [ProducesResponseType(200)]
        public IActionResult GetLicense([FromQuery]Request request)
        {
            var result = this.SubsonicService.GetLicense(request);
            return this.BuildResponse(request, result, "license");
        }

        [HttpGet("getMusicDirectory.view")]
        [HttpPost("getMusicDirectory.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetMusicDirectory([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetMusicDirectory(request, null);
            return this.BuildResponse(request, result, "directory");
        }

        [HttpGet("getMusicFolders.view")]
        [HttpPost("getMusicFolders.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetMusicFolders([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetMusicFolders(request);
            return this.BuildResponse(request, result, "musicFolders");
        }

        [HttpGet("getPlaylist.view")]
        [HttpPost("getPlaylist.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetPlaylist([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetPlaylist(request, null);
            return this.BuildResponse(request, result, "playlist");
        }

        [HttpGet("getPlaylists.view")]
        [HttpPost("getPlaylists.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetPlaylists([FromQuery]Request request, string username)
        {
            var result = await this.SubsonicService.GetPlaylists(request, null, username);
            return this.BuildResponse(request, result, "playlists");
        }

        [HttpGet("getPodcasts.view")]
        [HttpPost("getPodcasts.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetPodcasts([FromQuery]Request request, bool includeEpisodes)
        {
            var result = await this.SubsonicService.GetPodcasts(request);
            return this.BuildResponse(request, result, "podcasts");
        }

        [HttpGet("getRandomSongs.view")]
        [HttpPost("getRandomSongs.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetRandomSongs([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetRandomSongs(request, null);
            return this.BuildResponse(request, result, "randomSongs");
        }

        [HttpGet("getStarred.view")]
        [HttpPost("getStarred.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetStarred([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetStarred(request, null, StarredVersion.One);
            return this.BuildResponse(request, result, "starred");
        }

        [HttpGet("getStarred2.view")]
        [HttpPost("getStarred2.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetStarred2([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetStarred(request, null, StarredVersion.Two);
            return this.BuildResponse(request, result, "starred");
        }

        [HttpGet("getUser.view")]
        [HttpPost("getUser.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetUser([FromQuery]Request request, string username)
        {
            var result = await this.SubsonicService.GetUser(request, username);
            return this.BuildResponse(request, result, "user");
        }

        [HttpGet("ping.view")]
        [HttpPost("ping.view")]
        [ProducesResponseType(200)]
        public IActionResult Ping([FromQuery]Request request)
        {
            if (request.IsJSONRequest)
            {
                var result = this.SubsonicService.Ping(request);
                return this.BuildResponse(request, result);
            }
            return Content("<subsonic-response xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://subsonic.org/restapi\" status=\"ok\" version=\"1.16.0\" />", "application/xml");
        }

        /// <summary>
        /// Returns albums, artists and songs matching the given search criteria. Supports paging through the result.
        /// </summary>
        [HttpGet("search.view")]
        [HttpPost("search.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Search([FromQuery]Request request)
        {
            var result = await this.SubsonicService.Search(request, null, SearchVersion.One);
            return this.BuildResponse(request, result, "searchResult");
        }

        [HttpGet("search2.view")]
        [HttpPost("search2.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Search2([FromQuery]Request request)
        {
            var result = await this.SubsonicService.Search(request, null, SearchVersion.Two);
            return this.BuildResponse(request, result, "searchResult2");
        }

        [HttpGet("search3.view")]
        [HttpPost("search3.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Search3([FromQuery]Request request)
        {
            var result = await this.SubsonicService.Search(request, null, SearchVersion.Three);
            return this.BuildResponse(request, result, "searchResult3");
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
            var status = response?.status.ToString();
            var version = response?.version ?? Roadie.Api.Services.SubsonicService.SubsonicVersion;
            if (responseType == null)
            {
                return "{ \"subsonic-response\": { \"status\":\"" + status + "\", \"version\": \"" + version + "\" }}";
            }
            return "{ \"subsonic-response\": { \"status\":\"" + status + "\", \"version\": \"" + version + "\", \"" + responseType + "\":" + response != null ? JsonConvert.SerializeObject(response.Item) : string.Empty + "}}";
        }

        private IActionResult SendError(Request request, SubsonicOperationResult<Response> response = null, string responseType = null)
        {
            var version = response?.Data?.version ?? Roadie.Api.Services.SubsonicService.SubsonicVersion;
            string errorDescription = response?.ErrorCode?.DescriptionAttr();
            int? errorCode = (int?)response?.ErrorCode;
            if (request.IsJSONRequest)
            {
                this.Response.ContentType = "application/json";
                return Content("{ \"subsonic-response\": { \"status\":\"failed\", \"version\": \"" + version + "\", \"error\":{\"code\":\"" + errorCode + "\",\"message\":\"" + errorDescription + "\"}}}");
            }
            this.Response.ContentType = "application/xml";
            return Content($"<?xml version=\"1.0\" encoding=\"UTF-8\"?><subsonic-response xmlns=\"http://subsonic.org/restapi\" status=\"failed\" version=\"{ version }\"><error code=\"{ errorCode }\" message=\"{ errorDescription }\"/></subsonic-response>");
        }

        private IActionResult BuildResponse(Request request, SubsonicOperationResult<Response> response = null, string responseType = null)
        {
            var acceptHeader = this.Request.Headers["Accept"];
            this.Logger.LogTrace($"Subsonic Request: Method [{ this.Request.Method }], Accept Header [{ acceptHeader }], Path [{ this.Request.Path }], Query String [{ this.Request.QueryString }], Response Error Code [{ response.ErrorCode }], Request [{ JsonConvert.SerializeObject(request) }] ResponseType [{ responseType }]");
            if (response.ErrorCode.HasValue)
            {
                return this.SendError(request, response, responseType);
            }
            if (request.IsJSONRequest)
            {
                this.Response.ContentType = "application/json";
                return Content(this.BuildJsonResult(response.Data, responseType));
            }
            this.Response.ContentType = "application/xml";
            return Ok(response);
        }

        #endregion Response Builder Methods
    }
}