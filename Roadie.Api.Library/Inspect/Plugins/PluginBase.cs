using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.MetaData.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Roadie.Library.Inspect.Plugins
{
    public abstract class PluginBase : IInspectorPlugin
    {
        protected IRoadieSettings Configuration { get; }
        protected ICacheManager CacheManager { get; }
        protected ILogger Logger { get; }

        public abstract int Order { get; }
        public abstract string Description { get; }

        public PluginBase(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger)
        {
            this.Configuration = configuration;
            this.CacheManager = cacheManager;
            this.Logger = logger;
        }

        public abstract OperationResult<IEnumerable<AudioMetaData>> Process(IEnumerable<AudioMetaData> metaDatas);

    }
}
