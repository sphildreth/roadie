using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.MetaData.LastFm;
using Roadie.Library.Models.Users;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Library.Scrobble
{
    /// <summary>
    ///     Handles NowPlaying and Scrobbling for all integrations
    /// </summary>
    public class ScrobbleHandler : IScrobbleHandler
    {
        private IRoadieSettings Configuration { get; }

        private data.IRoadieDbContext DbContext { get; }

        private IHttpContext HttpContext { get; }

        private IHttpEncoder HttpEncoder { get; }

        private ILogger Logger { get; }

        private IEnumerable<IScrobblerIntegration> Scrobblers { get; }

        public ScrobbleHandler(IRoadieSettings configuration, ILogger<ScrobbleHandler> logger,
                                                            data.IRoadieDbContext dbContext,
            ICacheManager cacheManager, IHttpEncoder httpEncoder, IHttpContext httpContext)
        {
            Logger = logger;
            Configuration = configuration;
            DbContext = dbContext;
            HttpEncoder = httpEncoder;
            HttpContext = httpContext;
            var scrobblers = new List<IScrobblerIntegration>
            {
                new RoadieScrobbler(configuration, logger, dbContext, cacheManager, httpContext)
            };
            if (configuration.Integrations.LastFmProviderEnabled)
                scrobblers.Add(new LastFmHelper(configuration, cacheManager, logger, dbContext, httpEncoder));
            Scrobblers = scrobblers;
        }

        /// <summary>
        ///     Send Now Playing Requests
        /// </summary>
        public async Task<OperationResult<bool>> NowPlaying(User user, ScrobbleInfo scrobble)
        {
            var s = GetScrobbleInfoDetails(scrobble);
            foreach (var scrobbler in Scrobblers) await Task.Run(async () => await scrobbler.NowPlaying(user, s));
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
            var s = GetScrobbleInfoDetails(scrobble);
            foreach (var scrobbler in Scrobblers) await Task.Run(async () => await scrobbler.Scrobble(user, s));
            return new OperationResult<bool>
            {
                Data = true,
                IsSuccess = true
            };
        }

        private ScrobbleInfo GetScrobbleInfoDetails(ScrobbleInfo scrobble)
        {
            var scrobbleInfo = (from t in DbContext.Tracks
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
                                }).FirstOrDefault();

            scrobble.ArtistName = scrobbleInfo.ArtistName;
            scrobble.ReleaseTitle = scrobbleInfo.ReleaseTitle;
            scrobble.TrackTitle = scrobbleInfo.TrackTitle;
            scrobble.TrackNumber = scrobbleInfo.TrackNumber.ToString();
            scrobble.TrackDuration = TimeSpan.FromMilliseconds(scrobbleInfo.Duration ?? 0);
            return scrobble;
        }
    }
}