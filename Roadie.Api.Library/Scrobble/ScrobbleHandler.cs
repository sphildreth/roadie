using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Encoding;
using Roadie.Library.MetaData.LastFm;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.Scrobble
{
    /// <summary>
    ///     Handles NowPlaying and Scrobbling for all integrations
    /// </summary>
    public class ScrobbleHandler : IScrobbleHandler
    {
        private IRoadieSettings Configuration { get; }

        private IRoadieDbContext DbContext { get; }

        private IHttpContext HttpContext { get; }

        private IHttpEncoder HttpEncoder { get; }

        private ILogger Logger { get; }

        private IEnumerable<IScrobblerIntegration> Scrobblers { get; }

        public ScrobbleHandler(
            IRoadieSettings configuration,
            ILogger logger,
            IRoadieDbContext dbContext,
            ICacheManager cacheManager,
            IRoadieScrobbler roadieScrobbler)
        {
            Logger = logger;
            Configuration = configuration;
            DbContext = dbContext;
            var scrobblers = new List<IScrobblerIntegration>
            {
                roadieScrobbler
            };
            Scrobblers = scrobblers;
        }

        public ScrobbleHandler(
            IRoadieSettings configuration,
            ILogger<ScrobbleHandler> logger,
            IRoadieDbContext dbContext,
            ICacheManager cacheManager,
            IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            ILastFmHelper lastFmHelper,
            IRoadieScrobbler roadieScrobbler,
            ILastFMScrobbler lastFMScrobbler)
        {
            Logger = logger;
            Configuration = configuration;
            DbContext = dbContext;
            HttpEncoder = httpEncoder;
            HttpContext = httpContext;
            var scrobblers = new List<IScrobblerIntegration>
            {
                roadieScrobbler
            };
            if (configuration.Integrations.LastFmProviderEnabled)
            {
                scrobblers.Add(lastFMScrobbler);
            }
            Scrobblers = scrobblers;
        }

        private async Task<ScrobbleInfo> GetScrobbleInfoDetailsAsync(ScrobbleInfo scrobble)
        {
            var scrobbleInfo = await (from t in DbContext.Tracks
                                      join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                      join r in DbContext.Releases on rm.ReleaseId equals r.Id
                                      join a in DbContext.Artists on r.ArtistId equals a.Id
                                      where t.RoadieId == scrobble.TrackId
                                      select new
                                      {
                                          ArtistName = a.Name,
                                          ReleaseTitle = r.Title,
                                          TrackTitle = t.Title,
                                          t.TrackNumber,
                                          t.Duration
                                      }).FirstOrDefaultAsync().ConfigureAwait(false);

            scrobble.ArtistName = scrobbleInfo.ArtistName;
            scrobble.ReleaseTitle = scrobbleInfo.ReleaseTitle;
            scrobble.TrackTitle = scrobbleInfo.TrackTitle;
            scrobble.TrackNumber = scrobbleInfo.TrackNumber.ToString();
            scrobble.TrackDuration = TimeSpan.FromMilliseconds(scrobbleInfo.Duration ?? 0);
            return scrobble;
        }

        /// <summary>
        ///     Send Now Playing Requests
        /// </summary>
        public async Task<OperationResult<bool>> NowPlaying(User user, ScrobbleInfo scrobble)
        {
            var s = await GetScrobbleInfoDetailsAsync(scrobble).ConfigureAwait(false);
            foreach (var scrobbler in Scrobblers)
            {
                await scrobbler.NowPlaying(user, s).ConfigureAwait(false);
            }
            return new OperationResult<bool>
            {
                Data = true,
                IsSuccess = true
            };
        }

        /// <summary>
        ///     Send any Scrobble Requests
        /// </summary>
        public async Task<OperationResult<bool>> Scrobble(User user, ScrobbleInfo scrobble)
        {
            var s = await GetScrobbleInfoDetailsAsync(scrobble).ConfigureAwait(false);
            foreach (var scrobbler in Scrobblers.OrderBy(x => x.SortOrder))
            {
                await scrobbler.Scrobble(user, s).ConfigureAwait(false);
            }
            return new OperationResult<bool>
            {
                Data = true,
                IsSuccess = true
            };
        }
    }
}
