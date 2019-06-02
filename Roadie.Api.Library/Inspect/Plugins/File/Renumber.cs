using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.ID3Tags;
using System.Linq;

namespace Roadie.Library.Inspect.Plugins.File
{
    public class Renumber : FilePluginBase
    {
        public override string Description
        {
            get
            {
                return "Renumber: Renumber all given tracks sequentially and set maximum number of tracks";
            }
        }

        public override int Order { get; } = 1;

        public Renumber(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger, IID3TagsHelper tagsHelper)
            : base(configuration, cacheManager, logger, tagsHelper)
        {
        }

        public override OperationResult<AudioMetaData> Process(AudioMetaData metaData)
        {
            var result = new OperationResult<AudioMetaData>();
            var metaDatasForFilesInFolder = GetAudioMetaDatasForDirectory(metaData.FileInfo.Directory);
            metaData.TrackNumber = metaData.TrackNumber ?? ID3TagsHelper.DetermineTrackNumber(metaData.Filename);
            metaData.TotalTrackNumbers = ID3TagsHelper.DetermineTotalTrackNumbers(metaData.Filename) ?? metaDatasForFilesInFolder.Count();
            metaData.Disk = ID3TagsHelper.DetermineDiscNumber(metaData);
            metaData.TotalDiscCount = ID3TagsHelper.DetermineTotalDiscNumbers(metaDatasForFilesInFolder);
            result.Data = metaData;
            result.IsSuccess = true;
            return result;
        }
    }
}