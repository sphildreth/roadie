﻿using Microsoft.Extensions.Logging;
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

        public CleanUpTrackTitle(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger,
                            IID3TagsHelper tagsHelper)
            : base(configuration, cacheManager, logger, tagsHelper)
        {
        }

        public override OperationResult<AudioMetaData> Process(AudioMetaData metaData)
        {
            var result = new OperationResult<AudioMetaData>();
            if (Configuration.Processing.DoAudioCleanup)
            {
                var originalTitle = metaData.Title;
                metaData.Title = metaData.Title
                    ?.CleanString(Configuration, Configuration.Processing.TrackRemoveStringsRegex).ToTitleCase(false);
                if (string.IsNullOrEmpty(metaData.Title))
                {
                    metaData.Title = originalTitle;
                }
            }
            if (Configuration.Processing.DoDetectFeatureFragments)
            {
                if (!string.IsNullOrWhiteSpace(metaData?.Title))
                {
                    if (metaData.Release.HasFeaturingFragments())
                    {
                        throw new RoadieProcessingException($"Track title [{ metaData?.Title }] has Feature fragments.");
                    }
                }
            }
            result.Data = metaData;
            result.IsSuccess = true;
            return result;
        }
    }
}
