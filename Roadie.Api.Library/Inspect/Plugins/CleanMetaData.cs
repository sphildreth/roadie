using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.ID3Tags;
using System.Collections.Generic;
using System.Linq;


namespace Roadie.Library.Inspect.Plugins
{
    public class CleanMetaData : PluginBase
    {
        public override int Order => 2;

        public override string Description => "Clean: Clean the primary elements of MetaData";

        public CleanMetaData(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger)
            : base(configuration, cacheManager, logger)
        {
        }

        public override OperationResult<IEnumerable<AudioMetaData>> Process(IEnumerable<AudioMetaData> metaDatas)
        {
            var result = new OperationResult<IEnumerable<AudioMetaData>>();
            if (this.Configuration.Processing.DoAudioCleanup)
            {
                foreach (var metaData in metaDatas)
                {
                    metaData.Artist = metaData.Artist.CleanString(this.Configuration, this.Configuration.Processing.ArtistRemoveStringsRegex).ToTitleCase(doPutTheAtEnd: false);
                    metaData.Release = metaData.Release.CleanString(this.Configuration, this.Configuration.Processing.ReleaseRemoveStringsRegex).ToTitleCase(doPutTheAtEnd: false);
                    metaData.TrackArtist = metaData.TrackArtist.CleanString(this.Configuration, this.Configuration.Processing.ReleaseRemoveStringsRegex).ToTitleCase(doPutTheAtEnd: false);
                    metaData.Title = metaData.Title.CleanString(this.Configuration, this.Configuration.Processing.TrackRemoveStringsRegex).ToTitleCase(doPutTheAtEnd: false);
                }
            }
            result.Data = metaDatas;
            result.IsSuccess = true;
            return result;
        }
    }
}
