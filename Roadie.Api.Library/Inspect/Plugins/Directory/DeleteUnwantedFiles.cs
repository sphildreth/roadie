using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.MetaData.ID3Tags;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Roadie.Library.Inspect.Plugins.Directory
{
    public class DeleteUnwantedFiles : FolderPluginBase
    {
        public override string Description => "Cleanup: Delete unwanted files";

        public override bool IsEnabled => true;

        public override bool IsPostProcessingPlugin => true;

        public override int Order { get; } = 99;

        public DeleteUnwantedFiles(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger,
                                            IID3TagsHelper tagsHelper)
            : base(configuration, cacheManager, logger, tagsHelper)
        {
        }

        public override OperationResult<string> Process(DirectoryInfo directory)
        {
            var result = new OperationResult<string>();
            var data = string.Empty;

            var deletedFiles = new List<string>();
            var fileExtensionsToDelete = Configuration.FileExtensionsToDelete ?? new string[0];
            foreach (var file in directory.GetFiles("*.*", SearchOption.AllDirectories))
            {
                if (fileExtensionsToDelete.Any(x => x.Equals(file.Extension, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!Configuration.Inspector.IsInReadOnlyMode)
                    {
                        file.Delete();
                    }

                    deletedFiles.Add(file.Name);
                    Console.WriteLine($" X Deleted File [{file}], Was found in in FileExtensionsToDelete");
                }
            }
            result.Data = $"Deleted [{deletedFiles.Count()}] unwanted files";
            result.IsSuccess = true;
            return result;
        }
    }
}