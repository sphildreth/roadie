using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.ID3Tags;

namespace Roadie.Library.Inspect.Plugins.File
{
    public class EnsureFileWriteable : FilePluginBase
    {
        public override string Description => "Ensure: Ensure file is writable ";
        public override int Order => 1;

        public EnsureFileWriteable(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger, IID3TagsHelper tagsHelper)
            : base(configuration, cacheManager, logger, tagsHelper)
        {
        }

        public override OperationResult<AudioMetaData> Process(AudioMetaData metaData)
        {
            var result = new OperationResult<AudioMetaData>();
            if (this.Configuration.Processing.DoAudioCleanup)
            {
                if (metaData.FileInfo.IsReadOnly)
                {
                    metaData.FileInfo.Attributes = RemoveAttribute(metaData.FileInfo.Attributes, FileAttributes.ReadOnly);
                    Console.WriteLine($"╟ Removed read only attribute on file file [{ metaData.FileInfo.Name}");
                }
            }
            result.Data = metaData;
            result.IsSuccess = true;
            return result;
        }

        private static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }
    }
}
