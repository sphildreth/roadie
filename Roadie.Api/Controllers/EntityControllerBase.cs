﻿using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roadie.Api.Services;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Identity;
using Roadie.Library.Scrobble;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using models = Roadie.Library.Models.Users;

namespace Roadie.Api.Controllers
{
    public abstract class EntityControllerBase : Controller
    {
        public const string ControllerCacheRegionUrn = "urn:controller_cache";

        protected ICacheManager CacheManager { get; }

        protected ILogger Logger { get; set; }

        protected IRoadieSettings RoadieSettings { get; }

        protected UserManager<User> UserManager { get; }

        private models.User _currentUser;

        protected EntityControllerBase(
            ICacheManager cacheManager,
            IRoadieSettings roadieSettings,
            UserManager<User> userManager)
        {
            CacheManager = cacheManager;
            RoadieSettings = roadieSettings;
            UserManager = userManager;
        }

        protected async Task<models.User> CurrentUserModel()
        {
            if (_currentUser == null)
            {
                if (User.Identity.IsAuthenticated)
                {
                    _currentUser = await CacheManager.GetAsync($"urn:controller_user:{User.Identity.Name}", async () =>
                    {
                        return UserModelForUser(await UserManager.GetUserAsync(User).ConfigureAwait(false));
                    }, ControllerCacheRegionUrn).ConfigureAwait(false);
                }
            }
            if (_currentUser == null)
            {
                throw new UnauthorizedAccessException("Access Denied");
            }
            return _currentUser;
        }

        protected async Task<IActionResult> StreamTrack(Guid id, ITrackService trackService,
            IPlayActivityService playActivityService, models.User currentUser = null)
        {
            var sw = Stopwatch.StartNew();
            var timings = new Dictionary<string, long>();
            var tsw = new Stopwatch();

            tsw.Restart();
            var user = currentUser ?? await CurrentUserModel().ConfigureAwait(false); ;
            var track = trackService.StreamCheckAndInfo(user, id);
            if (track == null || (track?.IsNotFoundResult ?? false))
            {
                if (track?.Errors != null && (track?.Errors.Any() ?? false))
                {
                    Logger.LogCritical($"StreamTrack: ById Invalid For TrackId [{id}] OperationResult Errors [{string.Join('|', track?.Errors ?? new Exception[0])}], For User [{currentUser}]");
                }
                else
                {
                    Logger.LogCritical($"StreamTrack: ById Invalid For TrackId [{id}] OperationResult Messages [{string.Join('|', track?.Messages ?? new string[0])}], For User [{currentUser}]");
                }
                return NotFound("Unknown TrackId");
            }

            tsw.Stop();
            timings.Add("TrackService.StreamCheckAndInfo", tsw.ElapsedMilliseconds);
            tsw.Restart();

            var info = await trackService.TrackStreamInfoAsync(id,
                TrackService.DetermineByteStartFromHeaders(Request.Headers),
                TrackService.DetermineByteEndFromHeaders(Request.Headers, track.Data.FileSize),
                user).ConfigureAwait(false);
            if (!info?.IsSuccess ?? false || info?.Data == null)
            {
                if (info?.Errors != null && (info?.Errors.Any() ?? false))
                {
                    Logger.LogCritical(
                       $"StreamTrack: TrackStreamInfo Invalid For TrackId [{id}] OperationResult Errors [{string.Join('|', info?.Errors ?? new Exception[0])}], For User [{currentUser}]");
                }
                else
                {
                    Logger.LogCritical(
                       $"StreamTrack: TrackStreamInfo Invalid For TrackId [{id}] OperationResult Messages [{string.Join('|', info?.Messages ?? new string[0])}], For User [{currentUser}]");
                }

                return NotFound("Unknown TrackId");
            }

            tsw.Stop();
            timings.Add("TrackStreamInfo", tsw.ElapsedMilliseconds);

            tsw.Restart();
            Response.Headers.Add("Content-Disposition", info.Data.ContentDisposition);
            Response.Headers.Add("X-Content-Duration", info.Data.ContentDuration);
            Response.Headers.Add("Content-Duration", info.Data.ContentDuration);
            if (!info.Data.IsFullRequest)
            {
                Response.Headers.Add("Accept-Ranges", info.Data.AcceptRanges);
                Response.Headers.Add("Content-Range", info.Data.ContentRange);
            }

            Response.Headers.Add("Content-Length", info.Data.ContentLength);
            Response.ContentType = info.Data.ContentType;
            Response.StatusCode = info.Data.IsFullRequest ? (int)HttpStatusCode.OK : (int)HttpStatusCode.PartialContent;
            Response.Headers.Add("Last-Modified", info.Data.LastModified);
            if (!string.IsNullOrEmpty(info.Data.Etag))
            {
                Response.Headers.Add("ETag", info.Data.Etag);
            }
            Response.Headers.Add("Cache-Control", info.Data.CacheControl);
            if (!string.IsNullOrEmpty(info.Data.Pragma))
            {
                Response.Headers.Add("Pragma", info.Data.Pragma);
            }
            Response.Headers.Add("Expires", info.Data.Expires);

            await Response.Body.WriteAsync(info.Data.Bytes, 0, info.Data.Bytes.Length).ConfigureAwait(false);

            var scrobble = new ScrobbleInfo
            {
                IsRandomizedScrobble = false,
                TimePlayed = DateTime.UtcNow,
                TrackId = id
            };
            await playActivityService.NowPlayingAsync(user, scrobble).ConfigureAwait(false);
            sw.Stop();
            Logger.LogTrace($"StreamTrack ElapsedTime [{sw.ElapsedMilliseconds}], Timings [{CacheManager.CacheSerializer.Serialize(timings)}], StreamInfo `{info?.Data}`");
            return new EmptyResult();
        }

        protected models.User UserModelForUser(User user)
        {
            if (user == null)
            {
                return null;
            }

            var result = user.Adapt<models.User>();
            result.IsAdmin = User.IsInRole("Admin");
            result.IsEditor = User.IsInRole("Editor") || result.IsAdmin;
            return result;
        }
    }
}
