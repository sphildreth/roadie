using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.SearchEngines.MetaData;
using System;
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


        public FactoryBase(IRoadieSettings configuration, IRoadieDbContext context, ICacheManager cacheManager, ILogger logger, IHttpEncoder httpEncoder, IArtistLookupEngine artistLookupEngine, IReleaseLookupEngine releaseLookupEngine)
        {
            this.Configuration = configuration;
            this.DbContext = context;
            this.CacheManager = cacheManager;
            this.Logger = logger;
            this.HttpEncoder = httpEncoder;

            this.ArtistLookupEngine = artistLookupEngine;
            this.ReleaseLookupEngine = releaseLookupEngine;
        }

        protected async Task UpdateArtistCounts(int artistId, DateTime now)
        {
            var artist = this.DbContext.Artists.FirstOrDefault(x => x.Id == artistId);
            if (artist != null)
            {
                artist.ReleaseCount = this.DbContext.Releases.Where(x => x.ArtistId == artistId).Count();
                artist.TrackCount = (from r in this.DbContext.Releases
                                     join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                     join tr in this.DbContext.Tracks on rm.Id equals tr.ReleaseMediaId
                                     where r.ArtistId == artistId
                                     select tr).Count();
                artist.LastUpdated = now;
                await this.DbContext.SaveChangesAsync();
            }
        }

        protected async Task UpdateLabelCounts(int labelId, DateTime now)
        {
            var label = this.DbContext.Labels.FirstOrDefault(x => x.Id == labelId);
            if (label != null)
            {
                label.ReleaseCount = this.DbContext.ReleaseLabels.Where(x => x.LabelId == label.Id).Count();
                label.ArtistCount = (from r in this.DbContext.Releases
                                     join rl in this.DbContext.ReleaseLabels on r.Id equals rl.ReleaseId
                                     join a in this.DbContext.Artists on r.ArtistId equals a.Id
                                     where rl.LabelId == label.Id
                                     group a by a.Id into artists
                                     select artists).Select(x => x.Key).Count();
                label.TrackCount = (from r in this.DbContext.Releases
                                    join rl in this.DbContext.ReleaseLabels on r.Id equals rl.ReleaseId
                                    join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                    join t in this.DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                    where rl.LabelId == label.Id
                                    select t).Count();
                await this.DbContext.SaveChangesAsync();
            }
        }

        protected async Task UpdateReleaseCounts(int releaseId, DateTime now)
        {
            var release = this.DbContext.Releases.FirstOrDefault(x => x.Id == releaseId);
            if (release != null)
            {
                release.Duration = (from t in this.DbContext.Tracks
                                    join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                    where rm.ReleaseId == releaseId
                                    select t).Sum(x => x.Duration);
                await this.DbContext.SaveChangesAsync();
            }
        }
    }
}