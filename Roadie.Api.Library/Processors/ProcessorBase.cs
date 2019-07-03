using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Factories;
using Roadie.Library.MetaData.Audio;

namespace Roadie.Library.Processors
{
    public abstract class ProcessorBase
    {
        protected bool DoDeleteUnknowns => Configuration.Processing.DoDeleteUnknowns;

        protected bool DoMoveUnknowns => Configuration.Processing.DoMoveUnknowns;

        public IHttpEncoder HttpEncoder { get; }

        public int? SubmissionId { get; set; }

        protected string UnknownFolder => Configuration.Processing.UnknownFolder;

        protected IArtistFactory ArtistFactory { get; }

        protected IArtistLookupEngine ArtistLookupEngine { get; }

        protected IAudioMetaDataHelper AudioMetaDataHelper { get; }

        protected ICacheManager CacheManager { get; }

        protected IRoadieSettings Configuration { get; }

        protected IRoadieDbContext DbContext { get; }

        protected string DestinationRoot { get; }

        protected IImageFactory ImageFactory { get; }

        protected ILogger Logger { get; }

        protected IReleaseFactory ReleaseFactory { get; }

        protected IReleaseLookupEngine ReleaseLookupEngine { get; }

        public ProcessorBase(IRoadieSettings configuration, IHttpEncoder httpEncoder, string destinationRoot,
                                                                                                                                            IRoadieDbContext context, ICacheManager cacheManager,
            ILogger logger, IArtistLookupEngine artistLookupEngine, IArtistFactory artistFactory,
            IReleaseFactory releaseFactory, IImageFactory imageFactory, IReleaseLookupEngine releaseLookupEngine,
            IAudioMetaDataHelper audioMetaDataHelper)
        {
            Configuration = configuration;
            HttpEncoder = httpEncoder;
            DbContext = context;
            CacheManager = cacheManager;
            Logger = logger;

            DestinationRoot = destinationRoot;
            ArtistLookupEngine = artistLookupEngine;
            ReleaseLookupEngine = releaseLookupEngine;
            ArtistFactory = artistFactory;
            ReleaseFactory = releaseFactory;
            ImageFactory = imageFactory;
            AudioMetaDataHelper = audioMetaDataHelper;
        }
    }
}