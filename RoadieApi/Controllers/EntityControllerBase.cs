using Mapster;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Identity;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using models = Roadie.Library.Models.Users;

namespace Roadie.Api.Controllers
{
    public abstract class EntityControllerBase : ODataController
    {
        private models.User _currentUser = null;
        protected ILogger Logger { get; set; }
        protected ICacheManager CacheManager { get; }
        protected IConfiguration Configuration { get; }
        protected IRoadieSettings RoadieSettings { get; }
        protected UserManager<ApplicationUser> UserManager { get; }

        public EntityControllerBase(ICacheManager cacheManager, IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            this.CacheManager = cacheManager;
            this.Configuration = configuration;

            this.RoadieSettings = new RoadieSettings();
            this.Configuration.GetSection("RoadieSettings").Bind(this.RoadieSettings);
            this.UserManager = userManager;
        }

        protected async Task<models.User> CurrentUserModel()
        {
            if (this._currentUser == null)
            {
                if (this.User.Identity.IsAuthenticated)
                {
                    var user = await this.UserManager.GetUserAsync(User);
                    this._currentUser = this.UserModelForUser(user);
                }
            }
            return this._currentUser;
        }

        protected models.User UserModelForUser(ApplicationUser user)
        {
            var result = user.Adapt<models.User>();
            result.IsAdmin = User.IsInRole("Admin");
            result.IsEditor = User.IsInRole("Editor");
            return result;
        }

        protected async Task<IActionResult> StreamTrack(Guid id, ITrackService trackService, IPlayActivityService playActivityService, models.User currentUser = null)
        {
            var user = currentUser ?? await this.CurrentUserModel();
            var track = await trackService.ById(user, id, null);
            if (track == null || ( track?.IsNotFoundResult ?? false))
            {
                if (track?.Errors != null && (track?.Errors.Any() ?? false))
                {
                    this.Logger.LogCritical($"StreamTrack: ById Invalid For TrackId [{ id }] OperationResult Errors [{ string.Join('|', track?.Errors ?? new Exception[0]) }], For User [{ currentUser }]");
                }
                else
                {
                    this.Logger.LogCritical($"StreamTrack: ById Invalid For TrackId [{ id }] OperationResult Messages [{ string.Join('|', track?.Messages ?? new string[0]) }], For User [{ currentUser }]");
                }
                return NotFound("Unknown TrackId");
            }
            var info = await trackService.TrackStreamInfo(id,
                                                               Services.TrackService.DetermineByteStartFromHeaders(this.Request.Headers),
                                                               Services.TrackService.DetermineByteEndFromHeaders(this.Request.Headers, track.Data.FileSize));
            if (!info?.IsSuccess ?? false || info?.Data == null)
            {
                if (info?.Errors != null && (info?.Errors.Any() ?? false))
                {
                    this.Logger.LogCritical($"StreamTrack: TrackStreamInfo Invalid For TrackId [{ id }] OperationResult Errors [{ string.Join('|', info?.Errors ?? new Exception[0]) }], For User [{ currentUser }]");
                }
                else
                {
                    this.Logger.LogCritical($"StreamTrack: TrackStreamInfo Invalid For TrackId [{ id }] OperationResult Messages [{ string.Join('|', info?.Messages ?? new string[0]) }], For User [{ currentUser }]");
                }
                return NotFound("Unknown TrackId");
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
            var playListUser = await playActivityService.CreatePlayActivity(user, info.Data);
            this.Logger.LogInformation($"StreamTrack PlayActivity `{ playListUser?.Data.ToString() }`, StreamInfo `{ info.Data.ToString() }`");
            return new FileStreamResult(stream, info.Data.ContentType)
            {
                FileDownloadName = info.Data.FileName
            };
        }
    }
}