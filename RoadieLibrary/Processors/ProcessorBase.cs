using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Factories;


namespace Roadie.Library.Processors
{
    public abstract class ProcessorBase
    {
        protected readonly ICacheManager _cacheManager = null;
        protected readonly IRoadieSettings _configuration = null;
        protected readonly IRoadieDbContext _dbContext = null;
        protected readonly string _destinationRoot = null;
        protected readonly IHttpEncoder _httpEncoder = null;
        protected readonly ILogger _logger = null;
        protected ArtistFactory _artistFactory = null;
        protected ImageFactory _imageFactory = null;
        protected ReleaseFactory _releaseFactory = null;

        public IHttpEncoder HttpEncoder
        {
            get
            {
                return this._httpEncoder;
            }
        }

        public int? SubmissionId { get; set; }

        protected ArtistFactory ArtistFactory
        {
            get
            {
                return this._artistFactory ?? (this._artistFactory = new ArtistFactory(this.Configuration, this.HttpEncoder, this.DbContext, this.CacheManager, this.Logger));
            }
            set
            {
                this._artistFactory = value;
            }
        }

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

        protected string DestinationRoot
        {
            get
            {
                return this._destinationRoot;
            }
        }

        protected bool DoDeleteUnknowns
        {
            get
            {
                return this.Configuration.Processing.DoDeleteUnknowns;
            }
        }

        protected bool DoMoveUnknowns
        {
            get
            {
                return this.Configuration.Processing.DoMoveUnknowns;
            }
        }

        protected ImageFactory ImageFactory
        {
            get
            {
                return this._imageFactory ?? (this._imageFactory = new ImageFactory(this.Configuration, this.HttpEncoder, this.DbContext, this.CacheManager, this.Logger));
            }
            set
            {
                this._imageFactory = value;
            }
        }

        protected ILogger Logger
        {
            get
            {
                return this._logger;
            }
        }

        protected ReleaseFactory ReleaseFactory
        {
            get
            {
                return this._releaseFactory ?? (this._releaseFactory = new ReleaseFactory(this.Configuration, this.HttpEncoder, this.DbContext, this.CacheManager, this.Logger));
            }
            set
            {
                this._releaseFactory = value;
            }
        }

        protected string UnknownFolder
        {
            get
            {
                return this.Configuration.Processing.UnknownFolder;
            }
        }

        public ProcessorBase(IRoadieSettings configuration, IHttpEncoder httpEncoder, string destinationRoot, IRoadieDbContext context, ICacheManager cacheManager, ILogger logger)
        {
            this._configuration = configuration;
            this._httpEncoder = httpEncoder;
            this._dbContext = context;
            this._destinationRoot = destinationRoot;
            this._cacheManager = cacheManager;
            this._logger = logger;
        }
    }
}