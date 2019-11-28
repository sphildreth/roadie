using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadie.Api.ModelBinding;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.Models.ThirdPartyApi.Subsonic;
using Roadie.Library.Scrobble;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using User = Roadie.Library.Models.Users.User;

namespace Roadie.Api.Controllers
{
    [Route("subsonic/rest")]
    [ApiController]
    public class SubsonicController : EntityControllerBase
    {
        private IPlayActivityService PlayActivityService { get; }

        private IReleaseService ReleaseService { get; }

        private ISubsonicService SubsonicService { get; }

        /// <summary>
        ///     This is the user authenticated via the Subsonic methods - NOT the current API Identity User.
        /// </summary>
        private User SubsonicUser { get; set; }

        private ITrackService TrackService { get; }

        public SubsonicController(ISubsonicService subsonicService, ITrackService trackService,
                                                    IReleaseService releaseService,
            IPlayActivityService playActivityService, ILogger<SubsonicController> logger, ICacheManager cacheManager,
            UserManager<Library.Identity.User> userManager, IRoadieSettings roadieSettings)
            : base(cacheManager, roadieSettings, userManager)
        {
            Logger = logger;
            SubsonicService = subsonicService;
            TrackService = trackService;
            ReleaseService = releaseService;
            PlayActivityService = playActivityService;
        }

        [HttpGet("addChatMessage.view")]
        [HttpPost("addChatMessage.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> AddChatMessage(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.AddChatMessage(request, SubsonicUser);
            return BuildResponse(request, result);
        }

        [HttpGet("createBookmark.view")]
        [HttpPost("createBookmark.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> CreateBookmark(SubsonicRequest request, int position, string comment)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.CreateBookmark(request, SubsonicUser, position, comment);
            return BuildResponse(request, result);
        }

        [HttpGet("createPlaylist.view")]
        [HttpPost("createPlaylist.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> CreatePlaylist(SubsonicRequest request, string playlistId, string name,
            string[] songId)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.CreatePlaylist(request, SubsonicUser, name, songId, playlistId);
            return BuildResponse(request, result, "playlist");
        }

        [HttpGet("deleteBookmark.view")]
        [HttpPost("deleteBookmark.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteBookmark(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.DeleteBookmark(request, SubsonicUser);
            return BuildResponse(request, result);
        }

        [HttpGet("deletePlaylist.view")]
        [HttpPost("deletePlaylist.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeletePlaylist(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.DeletePlaylist(request, SubsonicUser);
            return BuildResponse(request, result);
        }

        [HttpGet("download.view")]
        [HttpPost("download.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Download(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return Unauthorized();
            var trackId = request.TrackId;
            if (trackId != null)
                return await base.StreamTrack(trackId.Value, TrackService, PlayActivityService, SubsonicUser);
            var releaseId = request.ReleaseId;
            if (releaseId != null)
            {
                var releaseZip = await ReleaseService.ReleaseZipped(SubsonicUser, releaseId.Value);
                if (!releaseZip.IsSuccess) return NotFound("Unknown Release id");
                return File(releaseZip.Data, "application/zip", (string)releaseZip.AdditionalData["ZipFileName"]);
            }

            return NotFound($"Unknown download id `{request.id}`");
        }

        [HttpGet("getAlbum.view")]
        [HttpPost("getAlbum.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAlbum(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetAlbum(request, SubsonicUser);
            return BuildResponse(request, result, "album");
        }

        [HttpGet("getAlbumInfo.view")]
        [HttpPost("getAlbumInfo.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAlbumInfo(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetAlbumInfo(request, SubsonicUser, AlbumInfoVersion.One);
            return BuildResponse(request, result, "albumInfo");
        }

        [HttpGet("getAlbumInfo2.view")]
        [HttpPost("getAlbumInfo2.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAlbumInfo2(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetAlbumInfo(request, SubsonicUser, AlbumInfoVersion.Two);
            return BuildResponse(request, result, "albumInfo");
        }

        [HttpGet("getAlbumList.view")]
        [HttpPost("getAlbumList.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAlbumList(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetAlbumList(request, SubsonicUser, AlbumListVersions.One);
            return BuildResponse(request, result, "albumList");
        }

        [HttpGet("getAlbumList2.view")]
        [HttpPost("getAlbumList2.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAlbumList2(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetAlbumList(request, SubsonicUser, AlbumListVersions.Two);
            return BuildResponse(request, result, "albumList");
        }

        [HttpGet("getArtist.view")]
        [HttpPost("getArtist.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetArtist(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetArtist(request, SubsonicUser);
            return BuildResponse(request, result, "artist");
        }

        [HttpGet("getArtistInfo.view")]
        [HttpPost("getArtistInfo.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetArtistInfo(SubsonicRequest request, int? count, bool? includeNotPresent)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result =
                await SubsonicService.GetArtistInfo(request, count, includeNotPresent ?? false, ArtistInfoVersion.One);
            return BuildResponse(request, result, "artistInfo");
        }

        [HttpGet("getArtistInfo2.view")]
        [HttpPost("getArtistInfo2.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetArtistInfo2(SubsonicRequest request, int? count, bool? includeNotPresent)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result =
                await SubsonicService.GetArtistInfo(request, count, includeNotPresent ?? false, ArtistInfoVersion.Two);
            return BuildResponse(request, result, "artistInfo2");
        }

        [HttpGet("getArtists.view")]
        [HttpPost("getArtists.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetArtists(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetArtists(request, SubsonicUser);
            return BuildResponse(request, result, "artists");
        }

        [HttpGet("getAvatar.view")]
        [HttpPost("getAvatar.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAvatar(SubsonicRequest request, string username)
        {
            var user = await UserManager.FindByNameAsync(username);
            return Redirect(
                $"/images/user/{user.RoadieId}/{RoadieSettings.ThumbnailImageSize.Width}/{RoadieSettings.ThumbnailImageSize.Height}");
        }

        [HttpGet("getBookmarks.view")]
        [HttpPost("getBookmarks.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetBookmarks(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetBookmarks(request, SubsonicUser);
            return BuildResponse(request, result, "bookmarks");
        }

        [HttpGet("getChatMessages.view")]
        [HttpPost("getChatMessages.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetChatMessages(SubsonicRequest request, long? since)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetChatMessages(request, SubsonicUser, since);
            return BuildResponse(request, result, "chatMessages");
        }

        [HttpGet("getCoverArt.view")]
        [HttpPost("getCoverArt.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetCoverArt(SubsonicRequest request, int? size)
        {
            var result = await SubsonicService.GetCoverArt(request, size);
            if (result == null || result.IsNotFoundResult) return NotFound();
            if (!result.IsSuccess)
            {
                Logger.LogWarning($"GetCoverArt Failed For [{JsonConvert.SerializeObject(request)}]");
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }

            return File(result.Data.Bytes,
                result.ContentType,
                $"{result.Data.Caption ?? request.id}.jpg",
                result.LastModified,
                result.ETag);
        }

        [HttpGet("getGenres.view")]
        [HttpPost("getGenres.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetGenres(SubsonicRequest request)
        {
            var result = await SubsonicService.GetGenres(request);
            return BuildResponse(request, result, "genres");
        }

        [HttpGet("getIndexes.view")]
        [HttpPost("getIndexes.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetIndexes(SubsonicRequest request, long? ifModifiedSince = null)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetIndexes(request, SubsonicUser, ifModifiedSince);
            return BuildResponse(request, result, "indexes");
        }

        [HttpGet("getLicense.view")]
        [HttpPost("getLicense.view")]
        [ProducesResponseType(200)]
        public IActionResult GetLicense(SubsonicRequest request)
        {
            var result = SubsonicService.GetLicense(request);
            return BuildResponse(request, result, "license");
        }

        [HttpGet("getLyrics.view")]
        [HttpPost("getLyrics.view")]
        [ProducesResponseType(200)]
        public IActionResult GetLyrics(SubsonicRequest request, string artist, string title)
        {
            var result = SubsonicService.GetLyrics(request, artist, title);
            return BuildResponse(request, result, "lyrics ");
        }

        [HttpGet("getMusicDirectory.view")]
        [HttpPost("getMusicDirectory.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetMusicDirectory(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetMusicDirectory(request, SubsonicUser);
            return BuildResponse(request, result, "directory");
        }

        [HttpGet("getMusicFolders.view")]
        [HttpPost("getMusicFolders.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetMusicFolders(SubsonicRequest request)
        {
            var result = await SubsonicService.GetMusicFolders(request);
            return BuildResponse(request, result, "musicFolders");
        }

        [HttpGet("getNowPlaying.view")]
        [HttpPost("getNowPlaying.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetNowPlaying(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetNowPlaying(request, SubsonicUser);
            return BuildResponse(request, result, "nowPlaying");
        }

        [HttpGet("getPlaylist.view")]
        [HttpPost("getPlaylist.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetPlaylist(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetPlaylist(request, SubsonicUser);
            return BuildResponse(request, result, "playlist");
        }

        [HttpGet("getPlaylists.view")]
        [HttpPost("getPlaylists.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetPlaylists(SubsonicRequest request, string username)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetPlaylists(request, SubsonicUser, username);
            return BuildResponse(request, result, "playlists");
        }

        [HttpGet("getPlayQueue.view")]
        [HttpPost("getPlayQueue.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetPlayQueue(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetPlayQueue(request, SubsonicUser);
            if (result.IsEmptyResponse) return BuildResponse(request, result);
            return BuildResponse(request, result, "playQueue");
        }

        [HttpGet("getPodcasts.view")]
        [HttpPost("getPodcasts.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetPodcasts(SubsonicRequest request, bool includeEpisodes)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetPodcasts(request);
            return BuildResponse(request, result, "podcasts");
        }

        [HttpGet("getRandomSongs.view")]
        [HttpPost("getRandomSongs.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetRandomSongs(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetRandomSongs(request, SubsonicUser);
            return BuildResponse(request, result, "randomSongs");
        }

        [HttpGet("getSimilarSongs.view")]
        [HttpPost("getSimilarSongs.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetSimilarSongs(SubsonicRequest request, int? count = 50)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetSimliarSongs(request, SubsonicUser, SimilarSongsVersion.One, count);
            return BuildResponse(request, result, "similarSongs");
        }

        [HttpGet("getSimilarSongs2.view")]
        [HttpPost("getSimilarSongs2.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetSimilarSongs2(SubsonicRequest request, int? count = 50)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetSimliarSongs(request, SubsonicUser, SimilarSongsVersion.Two, count);
            return BuildResponse(request, result, "similarSongs2");
        }

        [HttpGet("getSong.view")]
        [HttpPost("getSong.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetSong(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetSong(request, SubsonicUser);
            return BuildResponse(request, result, "song");
        }

        [HttpGet("getSongsByGenre.view")]
        [HttpPost("getSongsByGenre.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetSongsByGenre(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetSongsByGenre(request, SubsonicUser);
            return BuildResponse(request, result, "songsByGenre");
        }

        [HttpGet("getStarred.view")]
        [HttpPost("getStarred.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetStarred(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetStarred(request, SubsonicUser, StarredVersion.One);
            return BuildResponse(request, result, "starred");
        }

        [HttpGet("getStarred2.view")]
        [HttpPost("getStarred2.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetStarred2(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetStarred(request, SubsonicUser, StarredVersion.Two);
            return BuildResponse(request, result, "starred");
        }

        [HttpGet("getTopSongs.view")]
        [HttpPost("getTopSongs.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetTopSongs(SubsonicRequest request, int? count = 50)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetTopSongs(request, SubsonicUser, count);
            return BuildResponse(request, result, "topSongs");
        }

        [HttpGet("getUser.view")]
        [HttpPost("getUser.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetUser(SubsonicRequest request, string username)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.GetUser(request, username ?? request.u);
            return BuildResponse(request, result, "user");
        }

        [HttpGet("getVideos.view")]
        [HttpPost("getVideos.view")]
        [ProducesResponseType(200)]
        public IActionResult GetVideos(SubsonicRequest request)
        {
            var result = SubsonicService.GetVideos(request);
            return BuildResponse(request, result, "videos");
        }

        [HttpGet("ping.view")]
        [HttpPost("ping.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Ping(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            if (request.IsJSONRequest)
            {
                var result = SubsonicService.Ping(request);
                return BuildResponse(request, result);
            }

            return Content(
                "<subsonic-response xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://subsonic.org/restapi\" status=\"ok\" version=\"1.16.0\" />",
                "application/xml");
        }

        [HttpGet("savePlayQueue.view")]
        [HttpPost("savePlayQueue.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> SavePlayQueue(SubsonicRequest request, string username, string current,
            long? position)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.SavePlayQueue(request, SubsonicUser, current, position);
            return BuildResponse(request, result);
        }

        [HttpGet("scrobble.view")]
        [HttpGet("scrobble")]
        [HttpPost("scrobble.view")]
        [HttpPost("scrobble")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Scrobble(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var timePlayed = string.IsNullOrEmpty(request.time)
                ? DateTime.UtcNow.AddDays(-1)
                : SafeParser.ToNumber<long>(request.time).FromUnixTime();
            var scrobblerResponse = await PlayActivityService.Scrobble(SubsonicUser, new ScrobbleInfo
            {
                TrackId = request.TrackId.Value,
                TimePlayed = timePlayed
            });
            return BuildResponse(request, new SubsonicOperationResult<Response>
            {
                IsSuccess = scrobblerResponse.IsSuccess,
                Data = new Response()
            });
        }

        /// <summary>
        ///     Returns albums, artists and songs matching the given search criteria. Supports paging through the result.
        /// </summary>
        [HttpGet("search.view")]
        [HttpPost("search.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Search(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.Search(request, SubsonicUser, SearchVersion.One);
            return BuildResponse(request, result, "searchResult");
        }

        [HttpGet("search2.view")]
        [HttpPost("search2.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Search2(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.Search(request, SubsonicUser, SearchVersion.Two);
            return BuildResponse(request, result, "searchResult2");
        }

        [HttpGet("search3.view")]
        [HttpPost("search3.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Search3(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.Search(request, SubsonicUser, SearchVersion.Three);
            return BuildResponse(request, result, "searchResult3");
        }

        [HttpGet("setRating.view")]
        [HttpPost("setRating.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> SetRating(SubsonicRequest request, short rating)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.SetRating(request, SubsonicUser, rating);
            return BuildResponse(request, result);
        }

        [HttpGet("star.view")]
        [HttpPost("star.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Star(SubsonicRequest request, [FromQueryAttribute] string[] albumId,
            [FromQueryAttribute] string[] artistId)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.ToggleStar(request, SubsonicUser, true, albumId, artistId);
            return BuildResponse(request, result);
        }

        [HttpGet("stream.view")]
        [HttpPost("stream.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> StreamTrack(SubsonicRequest request)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return Unauthorized();
            var trackId = request.TrackId;
            if (trackId == null) return NotFound("Invalid TrackId");
            return await base.StreamTrack(trackId.Value, TrackService, PlayActivityService, SubsonicUser);
        }

        [HttpGet("unstar.view")]
        [HttpPost("unstar.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UnStar(SubsonicRequest request, [FromQueryAttribute] string[] albumId,
            [FromQueryAttribute] string[] artistId)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.ToggleStar(request, SubsonicUser, false, albumId, artistId);
            return BuildResponse(request, result);
        }

        [HttpGet("updatePlaylist.view")]
        [HttpPost("updatePlaylist.view")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpdatePlaylist(SubsonicRequest request, [FromQueryAttribute] string playlistId,
            [FromQueryAttribute] string name, [FromQueryAttribute] string comment, [FromQueryAttribute] bool? @public,
            [FromQueryAttribute] string[] songIdToAdd, [FromQueryAttribute] int[] songIndexToRemove)
        {
            var authResult = await AuthenticateUser(request);
            if (authResult != null) return authResult;
            var result = await SubsonicService.UpdatePlaylist(request, SubsonicUser, playlistId, name, comment, @public,
                songIdToAdd, songIndexToRemove);
            return BuildResponse(request, result);
        }

        private async Task<IActionResult> AuthenticateUser(SubsonicRequest request)
        {
            var appUser = await SubsonicService.Authenticate(request);
            if (!(appUser?.IsSuccess ?? false) || (appUser?.IsNotFoundResult ?? false))
            {
                return BuildResponse(request, appUser.Adapt<SubsonicOperationResult<Response>>());
            }
            SubsonicUser = UserModelForUser(appUser.Data.SubsonicUser);
            return null;
        }

        #region Response Builder Methods

        private IActionResult BuildResponse(SubsonicRequest request, SubsonicOperationResult<Response> response = null,
            string responseType = null)
        {
            var acceptHeader = Request.Headers["Accept"];
            string postBody = null;
            var queryString = Request.QueryString.ToString();
            string queryPath = Request.Path;
            var method = Request.Method;

            if (!Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(Request.ContentType))
            {
                var formCollection = Request.Form;
                var formDictionary = new Dictionary<string, object>();
                if (formCollection != null && formCollection.Any())
                    foreach (var form in formCollection)
                        if (!formDictionary.ContainsKey(form.Key))
                            formDictionary[form.Key] = form.Value.FirstOrDefault();
                postBody = JsonConvert.SerializeObject(formDictionary);
            }

            if (response?.ErrorCode.HasValue ?? false) return SendError(request, response);
            if (request.IsJSONRequest)
            {
                Response.ContentType = "application/json";
                var status = response?.Data?.status.ToString();
                var version = response?.Data?.version ?? Services.SubsonicService.SubsonicVersion;
                string jsonResult = null;
                if (responseType == null)
                    jsonResult = "{ \"subsonic-response\": { \"status\":\"" + status + "\", \"version\": \"" + version +
                                 "\" }}";
                else
                    jsonResult = "{ \"subsonic-response\": { \"status\":\"" + status + "\", \"version\": \"" + version +
                                 "\", \"" + responseType + "\":" + (response?.Data != null
                                     ? JsonConvert.SerializeObject(response.Data.Item)
                                     : string.Empty) + "}}";
                if ((request?.f ?? string.Empty).Equals("jsonp", StringComparison.OrdinalIgnoreCase))
                    jsonResult = request.callback + "(" + jsonResult + ");";
                return Content(jsonResult);
            }

            Response.ContentType = "application/xml";
            return Ok(response.Data);
        }

        private IActionResult SendError(SubsonicRequest request, SubsonicOperationResult<Response> response = null)
        {
            var version = response?.Data?.version ?? Services.SubsonicService.SubsonicVersion;
            var errorDescription = response?.ErrorCode?.DescriptionAttr();
            var errorCode = (int?)response?.ErrorCode;
            if (request.IsJSONRequest)
            {
                Response.ContentType = "application/json";
                return Content("{ \"subsonic-response\": { \"status\":\"failed\", \"version\": \"" + version +
                               "\", \"error\":{\"code\":\"" + errorCode + "\",\"message\":\"" + errorDescription +
                               "\"}}}");
            }

            Response.ContentType = "application/xml";
            return Content(
                $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><subsonic-response xmlns=\"http://subsonic.org/restapi\" status=\"failed\" version=\"{version}\"><error code=\"{errorCode}\" message=\"{errorDescription}\"/></subsonic-response>");
        }

        #endregion Response Builder Methods
    }
}