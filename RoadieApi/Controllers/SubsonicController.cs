using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Extensions;
using Roadie.Library.Identity;
using Roadie.Library.Models.ThirdPartyApi.Subsonic;
using Roadie.Library.Utility;
using System;
using System.IO;
using System.Linq;
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

        [HttpGet("getSimilarSongs.view")]
        [HttpPost("getSimilarSongs.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetSimilarSongs([FromQuery]Request request, int? count = 50)
        {
            var result = await this.SubsonicService.GetSimliarSongs(request, null, SimilarSongsVersion.One, count);
            return this.BuildResponse(request, result, "similarSongs");
        }

        [HttpGet("getSimilarSongs2.view")]
        [HttpPost("getSimilarSongs2.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetSimilarSongs2([FromQuery]Request request, int? count = 50)
        {
            var result = await this.SubsonicService.GetSimliarSongs(request, null, SimilarSongsVersion.Two, count);
            return this.BuildResponse(request, result, "similarSongs2");
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

        [HttpGet("getArtist.view")]
        [HttpPost("getArtist.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetArtist([FromQuery]Request request)
        {
            var postedForm = Request.Form.FirstOrDefault();
            request.id = request.id ?? (postedForm.Key == "id" ? postedForm.Value.FirstOrDefault() : string.Empty);
            var result = await this.SubsonicService.GetArtist(request, null);
            return this.BuildResponse(request, result, "artist");
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

        [HttpGet("getLyrics.view")]
        [HttpPost("getLyrics.view")]
        [ProducesResponseType(200)]
        public IActionResult GetLyrics([FromQuery]Request request, string artist, string title)
        {
            var result = this.SubsonicService.GetLyrics(request, artist, title);
            return this.BuildResponse(request, result, "lyrics ");
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

        [HttpGet("getSong.view")]
        [HttpPost("getSong.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetSong([FromQuery]Request request)
        {
            var result = await this.SubsonicService.GetSong(request, null);
            return this.BuildResponse(request, result, "song");
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

        [HttpGet("getTopSongs.view")]
        [HttpPost("getTopSongs.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetTopSongs([FromQuery]Request request, string artistId, int? count = 50)
        {
            var result = await this.SubsonicService.GetTopSongs(request, null, artistId, count);
            return this.BuildResponse(request, result, "topSongs");
        }

        [HttpGet("getUser.view")]
        [HttpPost("getUser.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetUser([FromQuery]Request request, string username)
        {
            var result = await this.SubsonicService.GetUser(request, username ?? request.u);
            return this.BuildResponse(request, result, "user");
        }

        [HttpGet("getVideos.view")]
        [HttpPost("getVideos.view")]
        [ProducesResponseType(200)]
        public IActionResult GetVideos([FromQuery]Request request)
        {
            var result = this.SubsonicService.GetVideos(request);
            return this.BuildResponse(request, result, "videos");
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

        private IActionResult BuildResponse(Request request, SubsonicOperationResult<Response> response = null, string responseType = null)
        {
            var acceptHeader = this.Request.Headers["Accept"];
            this.Logger.LogTrace($"Subsonic Request: Method [{ this.Request.Method }], Accept Header [{ acceptHeader }], Path [{ this.Request.Path }], Query String [{ this.Request.QueryString }], Response Error Code [{ response.ErrorCode }], Request [{ JsonConvert.SerializeObject(request) }] ResponseType [{ responseType }]");
            if (response?.ErrorCode.HasValue ?? false)
            {
                return this.SendError(request, response, responseType);
            }
            if (request.IsJSONRequest)
            {
                this.Response.ContentType = "application/json";
                var status = response?.Data?.status.ToString();
                var version = response?.Data?.version ?? Roadie.Api.Services.SubsonicService.SubsonicVersion;
                string jsonResult = null;
                if (responseType == null)
                {
                    jsonResult = "{ \"subsonic-response\": { \"status\":\"" + status + "\", \"version\": \"" + version + "\" }}";
                }
                else
                {
                    jsonResult = "{ \"subsonic-response\": { \"status\":\"" + status + "\", \"version\": \"" + version + "\", \"" + responseType + "\":" + (response?.Data != null ? JsonConvert.SerializeObject(response.Data.Item) : string.Empty) + "}}";
                }
                if((request?.f ?? string.Empty).Equals("jsonp", StringComparison.OrdinalIgnoreCase))
                {
                    jsonResult = request.callback + "(" + jsonResult + ");";
                }
                return Content(jsonResult);
            }
            this.Response.ContentType = "application/xml";
            return Ok(response.Data);
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

        #endregion Response Builder Methods
    }

    //[ModelBinder(BinderType = typeof(SubsonicRequestBinder))]
    //public class SubsonicRequest : Request
    //{
    //}

    //public class SubsonicRequestBinder : IModelBinder
    //{

    //    public Task BindModelAsync(ModelBindingContext bindingContext)
    //    {
    //        if (bindingContext == null)
    //        {
    //            throw new ArgumentNullException(nameof(bindingContext));
    //        }
    //        string valueFromBody = string.Empty;

    //        var queryDictionary = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(bindingContext.HttpContext.Request.QueryString.ToString());

    //        //using (var sr = new StreamReader(bindingContext.HttpContext.Request.Body))
    //        //{
    //        //    valueFromBody = sr.ReadToEnd();
    //        //}

    //        if (string.IsNullOrEmpty(valueFromBody) && (queryDictionary == null || !queryDictionary.Any()))
    //        {
    //            return Task.CompletedTask;
    //        }

    //        var model = new SubsonicRequest();
    //        model.u = queryDictionary.ContainsKey("u") ? queryDictionary["u"].First() : "";
    //        model.p = queryDictionary.ContainsKey("p") ? queryDictionary["p"].First() : "";
    //        model.s = queryDictionary.ContainsKey("s") ? queryDictionary["s"].First() : "";
    //        model.t = queryDictionary.ContainsKey("t") ? queryDictionary["t"].First() : "";
    //        model.v = queryDictionary.ContainsKey("v") ? queryDictionary["v"].First() : "";
    //        model.c = queryDictionary.ContainsKey("c") ? queryDictionary["c"].First() : "";
    //        model.id = queryDictionary.ContainsKey("id") ? queryDictionary["id"].First() : "";
    //        model.f = queryDictionary.ContainsKey("f") ? queryDictionary["f"].First() : "";
    //        model.callback = queryDictionary.ContainsKey("callback") ? queryDictionary["callback"].First() : "";
    //        model.MusicFolderId = queryDictionary.ContainsKey("MusicFolderId") ? SafeParser.ToNumber<int>(queryDictionary["MusicFolderId"].First()) : 0;
    //        model.Type = 

    //        var form = bindingContext.HttpContext.Request.Form.FirstOrDefault();
    //        switch (form.Key)
    //        {
    //            case "id":
    //                model.id = form.Value;
    //                break;
    //            default:
    //                break;
    //        }


    //        bindingContext.Result = ModelBindingResult.Success(model);

    //        return Task.CompletedTask;
    //    }
   // }

    //public class SubsonicRequestBinderProvider : IModelBinderProvider
    //{
    //    public IModelBinder GetBinder(ModelBinderProviderContext context)
    //    {
    //        if (context.Metadata.ModelType == typeof(Request))
    //        {
    //            return new SubsonicRequestBinder();
    //        }
    //        return null;
    //    }
    //}

}