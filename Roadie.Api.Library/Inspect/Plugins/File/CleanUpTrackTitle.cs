using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.ID3Tags;

namespace Roadie.Library.Inspect.Plugins.File
{
    public class CleanUpTrackTitle : FilePluginBase
    {
        public override string Description => "Clean: Clean Track Title (TIT2) ";
        public override int Order => 5;

        public CleanUpTrackTitle(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger, IID3TagsHelper tagsHelper)
            : base(configuration, cacheManager, logger, tagsHelper)
        {
        }

        public override OperationResult<AudioMetaData> Process(AudioMetaData metaData)
        {
            var result = new OperationResult<AudioMetaData>();
            if (this.Configuration.Processing.DoAudioCleanup)
            {
                var originalTitle = metaData.Title;
                metaData.Title = metaData.Title?.CleanString(this.Configuration, this.Configuration.Processing.TrackRemoveStringsRegex).ToTitleCase(doPutTheAtEnd: false);
                if(string.IsNullOrEmpty(metaData.Title))
                {
                    metaData.Title = originalTitle;
                }
            }
            result.Data = metaData;
            result.IsSuccess = true;
            return result;
        }
    }
}