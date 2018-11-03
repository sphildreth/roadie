using Roadie.Library.Caching;
using Roadie.Library.Factories;
using Roadie.Library.Utility;
using Roadie.Library.Logging;
using System.Linq;
using Roadie.Library.Data;
using Microsoft.Extensions.Configuration;

namespace Roadie.Library.Processors
{
    public abstract class ProcessorBase
    {
        protected readonly string _destinationRoot = null;
        protected readonly ICacheManager _cacheManager = null;
        protected readonly ILogger _logger = null;
        protected readonly IRoadieDbContext _dbContext = null;
        protected readonly IConfiguration _configuration = null;

        protected ArtistFactory _artistFactory = null;
        protected ReleaseFactory _releaseFactory = null;
        protected ImageFactory _imageFactory = null;

        public int? SubmissionId { get; set; }

        protected string DestinationRoot
        {
            get
            {
                return this._destinationRoot;
            }
        }

        protected ArtistFactory ArtistFactory
        {
            get
            {
                return this._artistFactory ?? (this._artistFactory = new ArtistFactory(this.Configuration, this.DbContext, this.CacheManager, this.LoggingService));
            }
            set
            {
                this._artistFactory = value;
            }
        }

        protected ReleaseFactory ReleaseFactory
        {
            get
            {
                return this._releaseFactory ?? (this._releaseFactory = new ReleaseFactory(this.Configuration, this.DbContext, this.CacheManager, this.LoggingService));
            }
            set
            {
                this._releaseFactory = value;
            }
        }

        protected ImageFactory ImageFactory
        {
            get
            {
                return this._imageFactory ?? (this._imageFactory = new ImageFactory(this.Configuration, this.DbContext, this.CacheManager, this.LoggingService));
            }
            set
            {
                this._imageFactory = value;
            }
        }


        protected bool DoMoveUnknowns
        {
            get
            {
                return SettingsHelper.Instance.Processing.DoMoveUnknowns;
            }
        }

        protected bool DoDeleteUnknowns
        {
            get
            {
                return SettingsHelper.Instance.Processing.DoDeleteUnknowns;
            }
        }

        protected string UnknownFolder
        {
            get
            {
                return SettingsHelper.Instance.Processing.UnknownFolder;
            }
        }
        protected ICacheManager CacheManager
        {
            get
            {
                return this._cacheManager;
            }
        }

        protected ILogger LoggingService
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

        public ProcessorBase(IConfiguration configuration, string destinationRoot, IRoadieDbContext context, ICacheManager cacheManager, ILogger logger)
        {
            this._configuration = configuration;
            this._dbContext = context;
            this._destinationRoot = destinationRoot;
            this._cacheManager = cacheManager;
            this._logger = logger;
        }
    }
}