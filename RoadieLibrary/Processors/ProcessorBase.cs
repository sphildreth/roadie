using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Factories;


namespace Roadie.Library.Processors
{
    public abstract class ProcessorBase
    {
        public IHttpEncoder HttpEncoder { get; }

        public int? SubmissionId { get; set; }

        protected IArtistFactory ArtistFactory { get; }

        protected ICacheManager CacheManager { get; }

        protected IRoadieSettings Configuration { get; }


        protected IRoadieDbContext DbContext { get; }


        protected string DestinationRoot { get; }


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

        protected IImageFactory ImageFactory { get; }

        protected ILogger Logger { get; }

        protected IReleaseFactory ReleaseFactory { get; }

        protected string UnknownFolder
        {
            get
            {
                return this.Configuration.Processing.UnknownFolder;
            }
        }

        protected IArtistLookupEngine ArtistLookupEngine { get; }

        public ProcessorBase(IRoadieSettings configuration, IHttpEncoder httpEncoder, string destinationRoot, IRoadieDbContext context, ICacheManager cacheManager, 
                             ILogger logger, IArtistLookupEngine artistLookupEngine, IArtistFactory artistFactory, IReleaseFactory releaseFactory, IImageFactory imageFactory)
        {
            this.Configuration = configuration;
            this.HttpEncoder = httpEncoder;
            this.DbContext = context;
            this.CacheManager = cacheManager;
            this.Logger = logger;

            this.DestinationRoot = destinationRoot;
            this.ArtistLookupEngine = artistLookupEngine;
            this.ArtistFactory = artistFactory;
            this.ReleaseFactory = releaseFactory;
            this.ImageFactory = imageFactory;
        }
    }
}