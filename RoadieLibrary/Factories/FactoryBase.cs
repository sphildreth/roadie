using Roadie.Library.Caching;
using Roadie.Library.SearchEngines.Imaging;
using Roadie.Library.SearchEngines.MetaData;
using Roadie.Library.SearchEngines.MetaData.Discogs;
using Roadie.Library.SearchEngines.MetaData.Spotify;
using Roadie.Library.SearchEngines.MetaData.Wikipedia;
using Roadie.Library.Logging;
using Roadie.Library.Data;
using Roadie.Library.MetaData.MusicBrainz;
using Roadie.Library.MetaData.LastFm;
using Microsoft.Extensions.Configuration;
using Roadie.Library.Encoding;

namespace Roadie.Library.Factories
{
    public abstract class FactoryBase
    {
        protected readonly IArtistSearchEngine _itunesArtistSearchEngine = null;
        protected readonly IArtistSearchEngine _musicBrainzyArtistSearchEngine = null;
        protected readonly IArtistSearchEngine _lastFmArtistSearchEngine = null;
        protected readonly IArtistSearchEngine _spotifyArtistSearchEngine = null;
        protected readonly IArtistSearchEngine _wikipediaArtistSearchEngine = null;
        protected readonly IArtistSearchEngine _discogsArtistSearchEngine = null;

        protected readonly IConfiguration _configuration = null;
        protected readonly ICacheManager _cacheManager = null;
        protected readonly ILogger _logger = null;
        protected readonly IRoadieDbContext _dbContext = null;
        protected readonly IHttpEncoder _httpEncoder = null;

        protected ICacheManager CacheManager
        {
            get
            {
                return this._cacheManager;
            }
        }

        protected IHttpEncoder HttpEncoder
        {
            get
            {
                return this._httpEncoder;
            }
        }

        protected ILogger Logger
        {
            get
            {
                return this._logger;
            }
        }

        protected IRoadieDbContext DbContext
        {
            get
            {
                return this._dbContext;
            }
        }

        protected IConfiguration Configuration
        {
            get
            {
                return this._configuration;
            }
        }

        protected IArtistSearchEngine ITunesArtistSearchEngine
        {
            get
            {
                return this._itunesArtistSearchEngine;
            }
        }

        protected IArtistSearchEngine MusicBrainzArtistSearchEngine
        {
            get
            {
                return this._musicBrainzyArtistSearchEngine;
            }
        }

        protected IArtistSearchEngine LastFmArtistSearchEngine
        {
            get
            {
                return this._lastFmArtistSearchEngine;
            }
        }

        protected IArtistSearchEngine SpotifyArtistSearchEngine
        {
            get
            {
                return this._spotifyArtistSearchEngine;
            }
        }

        protected IArtistSearchEngine WikipediaArtistSearchEngine
        {
            get
            {
                return this._wikipediaArtistSearchEngine;
            }
        }

        protected IArtistSearchEngine DiscogsArtistSearchEngine
        {
            get
            {
                return this._discogsArtistSearchEngine;
            }
        }

        protected IReleaseSearchEngine ITunesReleaseSearchEngine
        {
            get
            {
                return (IReleaseSearchEngine)this._itunesArtistSearchEngine;
            }
        }

        protected IReleaseSearchEngine MusicBrainzReleaseSearchEngine
        {
            get
            {
                return (IReleaseSearchEngine)this._musicBrainzyArtistSearchEngine;
            }
        }

        protected IReleaseSearchEngine LastFmReleaseSearchEngine
        {
            get
            {
                return (IReleaseSearchEngine)this._lastFmArtistSearchEngine;
            }
        }

        protected IReleaseSearchEngine SpotifyReleaseSearchEngine
        {
            get
            {
                return (IReleaseSearchEngine)this._spotifyArtistSearchEngine;
            }
        }

        protected IReleaseSearchEngine WikipediaReleaseSearchEngine
        {
            get
            {
                return (IReleaseSearchEngine)this._wikipediaArtistSearchEngine;
            }
        }

        protected IReleaseSearchEngine DiscogsReleaseSearchEngine
        {
            get
            {
                return (IReleaseSearchEngine)this._discogsArtistSearchEngine;
            }
        }

        protected ILabelSearchEngine DiscogsLabelSearchEngine
        {
            get
            {
                return (ILabelSearchEngine)this._discogsArtistSearchEngine;
            }
        }

        public FactoryBase(IConfiguration configuration, IRoadieDbContext context, ICacheManager cacheManager, ILogger logger, IHttpEncoder httpEncoder)
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