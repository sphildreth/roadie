using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.MetaData.ID3Tags;
using System.IO;

namespace Roadie.Library.Inspect.Plugins.Directory
{
    public abstract class FolderPluginBase : PluginBase, IInspectorDirectoryPlugin
    {
        public virtual bool IsEnabled => true;

        public virtual bool IsPostProcessingPlugin => false;

        public FolderPluginBase(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger,
                            IID3TagsHelper tagsHelper)
            : base(configuration, cacheManager, logger, tagsHelper)
        {
        }

        public abstract OperationResult<string> Process(DirectoryInfo directory);
    }
}
