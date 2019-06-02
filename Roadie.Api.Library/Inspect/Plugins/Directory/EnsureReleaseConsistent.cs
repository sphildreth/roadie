using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.MetaData.ID3Tags;
using System;
using System.IO;
using System.Linq;

namespace Roadie.Library.Inspect.Plugins.Directory
{
    public class EnsureReleaseConsistent : FolderPluginBase
    {
        public override string Description
        {
            get
            {
                return "Consistent: Ensure all MP3 files in folder have same Release Title (TALB)";
            }
        }

        public override int Order { get; } = 1;

        public EnsureReleaseConsistent(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger, IID3TagsHelper tagsHelper)
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
                var release = firstMetaData.Release;
                foreach (var metaData in metaDatasForFilesInFolder.Where(x => x.Release != release))
                {
                    modified++;
                    Console.WriteLine($"╟ Setting Release to [{ release }], was [{ metaData.Release }] on file [{ metaData.FileInfo.Name}");
                    metaData.Release = release;
                    if (!Configuration.Inspector.IsInReadOnlyMode)
                    {
                        TagsHelper.WriteTags(metaData, metaData.Filename);
                    }
                }
                data = $"Found [{ found }] files, Modified [{ modified }] files";
            }
            result.Data = data;
            result.IsSuccess = true;
            return result;
        }
    }
}