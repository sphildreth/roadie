using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;

namespace Roadie.Library.Factories
{
    public sealed class LabelFactory : FactoryBase, ILabelFactory
    {
        public LabelFactory(IRoadieSettings configuration, IHttpEncoder httpEncoder, IRoadieDbContext context,
            ICacheManager cacheManager, ILogger logger, IArtistLookupEngine artistLookupEngine,
            IReleaseLookupEngine releaseLookupEngine)
            : base(configuration, context, cacheManager, logger, httpEncoder, artistLookupEngine, releaseLookupEngine)
        {
        }

        // TODO Merge
    }
}