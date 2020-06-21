using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.MetaData.ID3Tags;
using System;
using System.IO;
using System.Linq;

namespace Roadie.Library.Inspect.Plugins.Directory
{
    public class EnsureYearConsistent : FolderPluginBase
    {
        public override string Description => "Consistent: Ensure all MP3 files in folder have same Year (TYER)";

        public override int Order { get; } = 1;

        public EnsureYearConsistent(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger,
                            IID3TagsHelper tagsHelper)
            : base(configuration, cacheManager, logger, tagsHelper)
        {
        }

        public override OperationResult<string> Process(DirectoryInfo directory)
        {
            var result = new OperationResult<string>();
            var data = string.Empty;
            var found = 0;
            var modified = 0;
            var metaDatasForFilesInFolder = GetAudioMetaDatasForDirectory(directory);
            if (metaDatasForFilesInFolder.Any())
            {
                found = metaDatasForFilesInFolder.Count();
                var firstMetaData = metaDatasForFilesInFolder.OrderBy(x => x.TrackNumber).First();
                var year = firstMetaData.Year;
                foreach (var metaData in metaDatasForFilesInFolder.Where(x => x.Year != year))
                {
                    modified++;
                    Console.WriteLine(
                        $"╟ Setting Year to [{year}], was [{metaData.Year}] on file [{metaData.FileInfo.Name}");
                    metaData.Year = year;
                    if (!Configuration.Inspector.IsInReadOnlyMode)
                    {
                        TagsHelper.WriteTags(metaData, metaData.Filename);
                    }
                }

                data = $"Found [{found}] files, Modified [{modified}] files";
            }

            result.Data = data;
            result.IsSuccess = true;
            return result;
        }
    }
}