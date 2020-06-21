using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.MetaData.ID3Tags;
using Roadie.Library.Utility;
using System;
using System.IO;
using System.Linq;

namespace Roadie.Library.Inspect.Plugins.Directory
{
    public class EnsureArtistConsistent : FolderPluginBase
    {
        public override string Description => "Consistent: Ensure all MP3 files in folder have same Artist (TPE1)";

        public override bool IsEnabled => false;

        public override int Order { get; } = 1;

        public EnsureArtistConsistent(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger,
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
                var firstMetaData = metaDatasForFilesInFolder.OrderBy(x => x.Filename ?? string.Empty)
                    .ThenBy(x => SafeParser.ToNumber<short>(x.TrackNumber)).FirstOrDefault();
                if (firstMetaData == null)
                {
                    return new OperationResult<string>("Error Getting First MetaData")
                    {
                        Data = $"Unable to read Metadatas for Directory [{directory.FullName}]"
                    };
                }

                var artist = firstMetaData.Artist;
                foreach (var metaData in metaDatasForFilesInFolder.Where(x => x.Artist != artist))
                {
                    modified++;
                    Console.WriteLine(
                        $"╟ Setting Artist to [{artist}], was [{metaData.Artist}] on file [{metaData.FileInfo.Name}");
                    metaData.Artist = artist;
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