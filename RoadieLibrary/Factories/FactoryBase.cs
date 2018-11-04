using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Logging;
using Roadie.Library.MetaData.LastFm;
using Roadie.Library.MetaData.MusicBrainz;
using Roadie.Library.SearchEngines.Imaging;
using Roadie.Library.SearchEngines.MetaData;
using Roadie.Library.SearchEngines.MetaData.Discogs;
using Roadie.Library.SearchEngines.MetaData.Spotify;
using Roadie.Library.SearchEngines.MetaData.Wikipedia;

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
    }
}