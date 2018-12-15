using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Extensions;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.Factories
{
    public sealed class LabelFactory : FactoryBase, ILabelFactory
    {
        public LabelFactory(IRoadieSettings configuration, IHttpEncoder httpEncoder, IRoadieDbContext context, ICacheManager cacheManager, ILogger logger, IArtistLookupEngine artistLookupEngine, IReleaseLookupEngine releaseLookupEngine)
            : base(configuration, context, cacheManager, logger, httpEncoder, artistLookupEngine, releaseLookupEngine)
        {
        }

        // TODO Merge

        

    }
}