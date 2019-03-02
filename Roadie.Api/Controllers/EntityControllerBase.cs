using Mapster;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Identity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using models = Roadie.Library.Models.Users;

namespace Roadie.Api.Controllers
{
    public abstract class EntityControllerBase : ODataController
    {
        public const string ControllerCacheRegionUrn = "urn:controller_cache";

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
                    this._currentUser = await this.CacheManager.GetAsync($"urn:controller_user:{ this.User.Identity.Name }", async () =>
                    {
                        return this.UserModelForUser(await this.UserManager.GetUserAsync(User));
                    }, ControllerCacheRegionUrn);                   
                }
            }
            if(this._currentUser == null)
            {
                throw new UnauthorizedAccessException("Access Denied");
            }
            return this._currentUser;
        }

        protected models.User UserModelForUser(ApplicationUser user)
        {
            if(user == null)
            {
                return null;
            }
            var result = user.Adapt<models.User>();
            result.IsAdmin = User.IsInRole("Admin");
            result.IsEditor = User.IsInRole("Editor") || result.IsAdmin;
            return result;
        }

        protected async Task<IActionResult> StreamTrack(Guid id, ITrackService trackService, IPlayActivityService playActivityService, models.User currentUser = null)
        {
            var sw = Stopwatch.StartNew();
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            tsw.Restart();
            var user = currentUser ?? await this.CurrentUserModel();
            var track = trackService.StreamCheckAndInfo(user, id);
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
            tsw.Stop();
            timings.Add("TrackService.StreamCheckAndInfo", tsw.ElapsedMilliseconds);
            tsw.Restart();

            var info = await trackService.TrackStreamInfo(id,
                                                        TrackService.DetermineByteStartFromHeaders(this.Request.Headers),
                                                        TrackService.DetermineByteEndFromHeaders(this.Request.Headers, track.Data.FileSize),
                                                        user);
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
            tsw.Stop();
            timings.Add("TrackStreamInfo", tsw.ElapsedMilliseconds);

            tsw.Restart();
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
            await Response.Body.WriteAsync(info.Data.Bytes, 0, info.Data.Bytes.Length);

            var playListUser = await playActivityService.CreatePlayActivity(user, info?.Data);
            sw.Stop();
            this.Logger.LogInformation($"StreamTrack ElapsedTime [{ sw.ElapsedMilliseconds }], Timings [{ JsonConvert.SerializeObject(timings) }] PlayActivity `{ playListUser?.Data.ToString() }`, StreamInfo `{ info?.Data.ToString() }`");
            return new EmptyResult();

        }

    }
}