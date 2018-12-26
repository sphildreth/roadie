using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Engines
{
    public abstract class LookupEngineBase
    {
        protected ICacheManager CacheManager { get; }
        protected IRoadieSettings Configuration { get; }
        protected IRoadieDbContext DbContext { get; }
        protected IHttpEncoder HttpEncoder { get; }
        protected ILogger Logger { get; }

        public LookupEngineBase(IRoadieSettings configuration, IHttpEncoder httpEncoder, IRoadieDbContext context, ICacheManager cacheManager, ILogger logger)
        {
            this.Configuration = configuration;
            this.HttpEncoder = httpEncoder;
            this.DbContext = context;
            this.CacheManager = cacheManager;
            this.Logger = logger;
        }
    }
}
