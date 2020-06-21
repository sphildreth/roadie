using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.ID3Tags;

namespace Roadie.Library.Inspect.Plugins.File
{
    public class CleanUpComments : FilePluginBase
    {
        public override string Description => "Clean: Clear Comments (COMM)";

        public override int Order => 5;

        public CleanUpComments(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger,
                            IID3TagsHelper tagsHelper)
            : base(configuration, cacheManager, logger, tagsHelper)
        {
        }

        public override OperationResult<AudioMetaData> Process(AudioMetaData metaData)
        {
            var result = new OperationResult<AudioMetaData>();
            if (Configuration.Processing.DoAudioCleanup)
            {
                if (Configuration.Processing.DoClearComments)
                {
                    metaData.Comments = null;
                }
            }

            result.Data = metaData;
            result.IsSuccess = true;
            return result;
        }
    }
}