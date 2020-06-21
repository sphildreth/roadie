using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.ID3Tags;

namespace Roadie.Library.Inspect.Plugins.File
{
    public class CleanUpReleaseTitle : FilePluginBase
    {
        public override string Description => "Clean: Release Title (TALB)";

        public override int Order => 5;

        public CleanUpReleaseTitle(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger,
                            IID3TagsHelper tagsHelper)
            : base(configuration, cacheManager, logger, tagsHelper)
        {
        }

        public override OperationResult<AudioMetaData> Process(AudioMetaData metaData)
        {
            var result = new OperationResult<AudioMetaData>();
            if (Configuration.Processing.DoAudioCleanup)
            {
                var originalRelease = metaData.Release;
                metaData.Release = metaData.Release
                    ?.CleanString(Configuration, Configuration.Processing.ReleaseRemoveStringsRegex).ToTitleCase(false);
                if (string.IsNullOrEmpty(metaData.Release))
                {
                    metaData.Release = originalRelease;
                }
            }
            if (Configuration.Processing.DoDetectFeatureFragments)
            {
                if (!string.IsNullOrWhiteSpace(metaData?.Release))
                {
                    if (metaData.Release.HasFeaturingFragments())
                    {
                        throw new RoadieProcessingException($"Release title [{ metaData?.Release }] has Feature fragments.");
                    }
                }
            }
            result.Data = metaData;
            result.IsSuccess = true;
            return result;
        }
    }
}