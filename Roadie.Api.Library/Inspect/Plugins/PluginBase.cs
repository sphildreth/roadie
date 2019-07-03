using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.ID3Tags;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Roadie.Library.Inspect.Plugins
{
    public abstract class PluginBase
    {
        public abstract string Description { get; }

        public abstract int Order { get; }

        protected ICacheManager CacheManager { get; }

        protected IRoadieSettings Configuration { get; }

        protected IEnumerable<string> ListReplacements { get; } = new List<string>
            {" ; ", " ;", "; ", ";", " & ", " &", "& ", ";", "&"};

        protected ILogger Logger { get; }

        protected IID3TagsHelper TagsHelper { get; }

        private Dictionary<string, IEnumerable<AudioMetaData>> CachedAudioDatas { get; }

        public PluginBase(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger,
                                                                            IID3TagsHelper tagsHelper)
        {
            Configuration = configuration;
            CacheManager = cacheManager;
            Logger = logger;
            TagsHelper = tagsHelper;
            CachedAudioDatas = new Dictionary<string, IEnumerable<AudioMetaData>>();
        }

        protected IEnumerable<AudioMetaData> GetAudioMetaDatasForDirectory(DirectoryInfo directory)
        {
            try
            {
                if (!CachedAudioDatas.ContainsKey(directory.FullName))
                {
                    var filesInMetaDataFolder = directory.GetFiles("*.mp3", SearchOption.TopDirectoryOnly);
                    var metaDatasForFilesInFolder = new List<AudioMetaData>();
                    foreach (var fileInMetaDataFolder in filesInMetaDataFolder)
                    {
                        var metaData = TagsHelper.MetaDataForFile(fileInMetaDataFolder.FullName, true);
                        metaDatasForFilesInFolder.Add(metaData.Data);
                    }

                    CachedAudioDatas.Add(directory.FullName, metaDatasForFilesInFolder);
                }

                return CachedAudioDatas[directory.FullName];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            return Enumerable.Empty<AudioMetaData>();
        }
    }
}