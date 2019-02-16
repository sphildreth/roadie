using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.ID3Tags;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Roadie.Library.Inspect.Plugins
{
    public class Renumber : PluginBase
    {
        public override string Description
        {
            get
            {
                return "Renumber: Renumber all given tracks sequentially and set maximum number of tracks";
            }
        }

        public override int Order { get; } = 1;

        public Renumber(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger)
            : base(configuration, cacheManager, logger)
        {
        }

        public override OperationResult<IEnumerable<AudioMetaData>> Process(IEnumerable<AudioMetaData> metaDatas)
        {
            var result = new OperationResult<IEnumerable<AudioMetaData>>();
            var totalNumberOfMedia = ID3TagsHelper.DetermineTotalDiscNumbers(metaDatas);
            var folders = metaDatas.GroupBy(x => x.FileInfo.DirectoryName);
            foreach(var folder in folders)
            {
                short looper = 0;
                foreach(var metaData in folder)
                {
                    looper++;
                    metaData.TrackNumber = looper;
                    metaData.TotalTrackNumbers = ID3TagsHelper.DetermineTotalTrackNumbers(metaData.Filename) ?? folder.Count();
                    metaData.Disk = ID3TagsHelper.DetermineDiscNumber(metaData);
                    metaData.TotalDiscCount = totalNumberOfMedia;
                }
            }
            result.Data = metaDatas;
            result.IsSuccess = true;
            return result;
        }


    }
}