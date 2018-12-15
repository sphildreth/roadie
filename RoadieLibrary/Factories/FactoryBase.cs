using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.MetaData.LastFm;
using Roadie.Library.MetaData.MusicBrainz;
using Roadie.Library.SearchEngines.Imaging;
using Roadie.Library.SearchEngines.MetaData;
using Roadie.Library.SearchEngines.MetaData.Discogs;
using Roadie.Library.SearchEngines.MetaData.Spotify;
using Roadie.Library.SearchEngines.MetaData.Wikipedia;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.Factories
{
    public abstract class FactoryBase
    {
        protected readonly ICacheManager _cacheManager = null;
        protected readonly IRoadieSettings _configuration = null;
        protected readonly IRoadieDbContext _dbContext = null;
        protected readonly IArtistSearchEngine _discogsArtistSearchEngine = null;
        protected readonly IHttpEncoder _httpEncoder = null;
        protected readonly IArtistSearchEngine _itunesArtistSearchEngine = null;
        protected readonly IArtistSearchEngine _lastFmArtistSearchEngine = null;
        protected readonly ILogger _logger = null;
        protected readonly IArtistSearchEngine _musicBrainzyArtistSearchEngine = null;
        protected readonly IArtistSearchEngine _spotifyArtistSearchEngine = null;
        protected readonly IArtistSearchEngine _wikipediaArtistSearchEngine = null;

        protected ICacheManager CacheManager
        {
            get
            {
                return this._cacheManager;
            }
        }

        protected IRoadieSettings Configuration
        {
            get
            {
                return this._configuration;
            }
        }

        protected IRoadieDbContext DbContext
        {
            get
            {
                return this._dbContext;
            }
        }

        protected IArtistSearchEngine DiscogsArtistSearchEngine
        {
            get
            {
                return this._discogsArtistSearchEngine;
            }
        }

        protected ILabelSearchEngine DiscogsLabelSearchEngine
        {
            get
            {
                return (ILabelSearchEngine)this._discogsArtistSearchEngine;
            }
        }

        protected IReleaseSearchEngine DiscogsReleaseSearchEngine
        {
            get
            {
                return (IReleaseSearchEngine)this._discogsArtistSearchEngine;
            }
        }

        protected IHttpEncoder HttpEncoder
        {
            get
            {
                return this._httpEncoder;
            }
        }

        protected IArtistSearchEngine ITunesArtistSearchEngine
        {
            get
            {
                return this._itunesArtistSearchEngine;
            }
        }

        protected IReleaseSearchEngine ITunesReleaseSearchEngine
        {
            get
            {
                return (IReleaseSearchEngine)this._itunesArtistSearchEngine;
            }
        }

        protected IArtistSearchEngine LastFmArtistSearchEngine
        {
            get
            {
                return this._lastFmArtistSearchEngine;
            }
        }

        protected IReleaseSearchEngine LastFmReleaseSearchEngine
        {
            get
            {
                return (IReleaseSearchEngine)this._lastFmArtistSearchEngine;
            }
        }

        protected ILogger Logger
        {
            get
            {
                return this._logger;
            }
        }

        protected IArtistSearchEngine MusicBrainzArtistSearchEngine
        {
            get
            {
                return this._musicBrainzyArtistSearchEngine;
            }
        }

        protected IReleaseSearchEngine MusicBrainzReleaseSearchEngine
        {
            get
            {
                return (IReleaseSearchEngine)this._musicBrainzyArtistSearchEngine;
            }
        }

        protected IArtistSearchEngine SpotifyArtistSearchEngine
        {
            get
            {
                return this._spotifyArtistSearchEngine;
            }
        }

        protected IReleaseSearchEngine SpotifyReleaseSearchEngine
        {
            get
            {
                return (IReleaseSearchEngine)this._spotifyArtistSearchEngine;
            }
        }

        protected IArtistSearchEngine WikipediaArtistSearchEngine
        {
            get
            {
                return this._wikipediaArtistSearchEngine;
            }
        }

        protected IReleaseSearchEngine WikipediaReleaseSearchEngine
        {
            get
            {
                return (IReleaseSearchEngine)this._wikipediaArtistSearchEngine;
            }
        }

        public FactoryBase(IRoadieSettings configuration, IRoadieDbContext context, ICacheManager cacheManager, ILogger logger, IHttpEncoder httpEncoder)
        {
            this._configuration = configuration;
            this._dbContext = context;
            this._cacheManager = cacheManager;
            this._logger = logger;
            this._httpEncoder = httpEncoder;

            this._itunesArtistSearchEngine = new ITunesSearchEngine(this.Configuration, this.CacheManager, this.Logger);
            this._musicBrainzyArtistSearchEngine = new MusicBrainzProvider(this.Configuration, this.CacheManager, this.Logger);
            this._lastFmArtistSearchEngine = new LastFmHelper(this.Configuration, this.CacheManager, this.Logger);
            this._spotifyArtistSearchEngine = new SpotifyHelper(this.Configuration, this.CacheManager, this.Logger);
            this._wikipediaArtistSearchEngine = new WikipediaHelper(this.Configuration, this.CacheManager, this.Logger, this.HttpEncoder);
            this._discogsArtistSearchEngine = new DiscogsHelper(this.Configuration, this.CacheManager, this.Logger);
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
            if(label != null)
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

    }
}