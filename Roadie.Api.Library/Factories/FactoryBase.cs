using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.SearchEngines.MetaData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.Factories
{
    public abstract class FactoryBase
    {
        protected IArtistLookupEngine ArtistLookupEngine { get; }

        protected ICacheManager CacheManager { get; }

        protected IRoadieSettings Configuration { get; }

        protected IRoadieDbContext DbContext { get; }

        protected ILabelSearchEngine DiscogsLabelSearchEngine { get; }

        protected IHttpEncoder HttpEncoder { get; }

        protected ILogger Logger { get; }

        protected IReleaseLookupEngine ReleaseLookupEngine { get; }

        public FactoryBase(IRoadieSettings configuration, IRoadieDbContext context, ICacheManager cacheManager,
                                                                            ILogger logger, IHttpEncoder httpEncoder, IArtistLookupEngine artistLookupEngine,
            IReleaseLookupEngine releaseLookupEngine)
        {
            Configuration = configuration;
            DbContext = context;
            CacheManager = cacheManager;
            Logger = logger;
            HttpEncoder = httpEncoder;

            ArtistLookupEngine = artistLookupEngine;
            ReleaseLookupEngine = releaseLookupEngine;
        }

        [Obsolete("Use Service Methods")]
        protected IEnumerable<int> ArtistIdsForRelease(int releaseId)
        {
            var trackArtistIds = (from r in DbContext.Releases
                                  join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                  join tr in DbContext.Tracks on rm.Id equals tr.ReleaseMediaId
                                  where r.Id == releaseId
                                  where tr.ArtistId != null
                                  select tr.ArtistId.Value).ToList();
            trackArtistIds.Add(DbContext.Releases.FirstOrDefault(x => x.Id == releaseId).ArtistId);
            return trackArtistIds.Distinct().ToArray();
        }

        [Obsolete("Use Service Methods")]
        protected async Task UpdateArtistCounts(int artistId, DateTime now)
        {
            var artist = DbContext.Artists.FirstOrDefault(x => x.Id == artistId);
            if (artist != null)
            {
                artist.ReleaseCount = DbContext.Releases.Where(x => x.ArtistId == artistId).Count();
                artist.TrackCount = (from r in DbContext.Releases
                                     join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                     join tr in DbContext.Tracks on rm.Id equals tr.ReleaseMediaId
                                     where tr.ArtistId == artistId || r.ArtistId == artistId
                                     select tr).Count();

                artist.LastUpdated = now;
                await DbContext.SaveChangesAsync();
                CacheManager.ClearRegion(artist.CacheRegion);
            }
        }

        [Obsolete("Use Service Methods")]
        /// <summary>
        /// Update the counts for all artists on a release (both track and release artists)
        /// </summary>
        protected async Task UpdateArtistCountsForRelease(int releaseId, DateTime now)
        {
            foreach (var artistId in ArtistIdsForRelease(releaseId)) await UpdateArtistCounts(artistId, now);
        }

        [Obsolete("Use Service Methods")]
        protected async Task UpdateLabelCounts(int labelId, DateTime now)
        {
            var label = DbContext.Labels.FirstOrDefault(x => x.Id == labelId);
            if (label != null)
            {
                label.ReleaseCount = DbContext.ReleaseLabels.Where(x => x.LabelId == label.Id).Count();
                label.ArtistCount = (from r in DbContext.Releases
                                     join rl in DbContext.ReleaseLabels on r.Id equals rl.ReleaseId
                                     join a in DbContext.Artists on r.ArtistId equals a.Id
                                     where rl.LabelId == label.Id
                                     group a by a.Id
                    into artists
                                     select artists).Select(x => x.Key).Count();
                label.TrackCount = (from r in DbContext.Releases
                                    join rl in DbContext.ReleaseLabels on r.Id equals rl.ReleaseId
                                    join rm in DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                    join t in DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                    where rl.LabelId == label.Id
                                    select t).Count();
                await DbContext.SaveChangesAsync();
                CacheManager.ClearRegion(label.CacheRegion);
            }
        }

        [Obsolete("Use Service Methods")]
        protected async Task UpdateReleaseCounts(int releaseId, DateTime now)
        {
            var release = DbContext.Releases.FirstOrDefault(x => x.Id == releaseId);
            if (release != null)
            {
                release.Duration = (from t in DbContext.Tracks
                                    join rm in DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                    where rm.ReleaseId == releaseId
                                    select t).Sum(x => x.Duration);
                await DbContext.SaveChangesAsync();
                CacheManager.ClearRegion(release.CacheRegion);
            }
        }
    }
}