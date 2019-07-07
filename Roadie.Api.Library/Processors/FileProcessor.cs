using Microsoft.Extensions.Logging;
using MimeMapping;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Extensions;
using Roadie.Library.FilePlugins;
using Roadie.Library.MetaData.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.Processors
{
    public sealed class FileProcessor : IFileProcessor
    {
        private bool DoDeleteUnknowns => Configuration.Processing.DoDeleteUnknowns;

        private bool DoMoveUnknowns => Configuration.Processing.DoMoveUnknowns;

        public IHttpEncoder HttpEncoder { get; }

        public int? SubmissionId { get; set; }

        private string UnknownFolder => Configuration.Processing.UnknownFolder;

        private IArtistLookupEngine ArtistLookupEngine { get; }

        private IAudioMetaDataHelper AudioMetaDataHelper { get; }

        private ICacheManager CacheManager { get; }

        private IRoadieSettings Configuration { get; }

        private IRoadieDbContext DbContext { get; }

        private ILogger Logger { get; }

        private IReleaseLookupEngine ReleaseLookupEngine { get; }

        private IEnumerable<IFilePlugin> _plugins;

        public IEnumerable<IFilePlugin> Plugins
        {
            get
            {
                if (_plugins == null)
                {
                    var plugins = new List<IFilePlugin>();
                    try
                    {
                        var type = typeof(IFilePlugin);
                        var types = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(s => s.GetTypes())
                            .Where(p => type.IsAssignableFrom(p));
                        foreach (var t in types)
                            if (t.GetInterface("IFilePlugin") != null && !t.IsAbstract && !t.IsInterface)
                            {
                                var plugin = Activator.CreateInstance(t, Configuration, HttpEncoder, CacheManager, Logger, ArtistLookupEngine,
                                    ReleaseLookupEngine, AudioMetaDataHelper) as IFilePlugin;
                                plugins.Add(plugin);
                            }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex);
                    }

                    _plugins = plugins.ToArray();
                }

                return _plugins;
            }
        }

        public FileProcessor(IRoadieSettings configuration, IHttpEncoder httpEncoder, 
                IRoadieDbContext context, ICacheManager cacheManager, ILogger<FileProcessor> logger,
                IArtistLookupEngine artistLookupEngine, IReleaseLookupEngine releaseLookupEngine,
                IAudioMetaDataHelper audioMetaDataHelper)
        {
            Configuration = configuration;
            HttpEncoder = httpEncoder;
            DbContext = context;
            CacheManager = cacheManager;
            Logger = logger;

            ArtistLookupEngine = artistLookupEngine;
            ReleaseLookupEngine = releaseLookupEngine;
            AudioMetaDataHelper = audioMetaDataHelper;
        }

        public static string DetermineFileType(FileInfo fileinfo)
        {
            var r = MimeUtility.GetMimeMapping(fileinfo.FullName);
            if (r.Equals("application/octet-stream"))
            {
                if (fileinfo.Extension.Equals(".cue")) r = "audio/r-cue";
                if (fileinfo.Extension.Equals(".mp4") || fileinfo.Extension.Equals(".m4a")) r = "audio/mp4";
            }

            Trace.WriteLine(string.Format("FileType [{0}] For File [{1}]", r, fileinfo.FullName));
            return r;
        }

        public async Task<OperationResult<bool>> Process(string filename, bool doJustInfo = false)
        {
            return await Process(new FileInfo(filename), doJustInfo);
        }

        public async Task<OperationResult<bool>> Process(FileInfo fileInfo, bool doJustInfo = false)
        {
            var result = new OperationResult<bool>();

            try
            {
                // Determine what type of file this is
                var fileType = DetermineFileType(fileInfo);

                OperationResult<bool> pluginResult = null;
                foreach (var p in Plugins)
                    // See if there is a plugin
                    if (p.HandlesTypes.Contains(fileType))
                    {
                        pluginResult = await p.Process(fileInfo, doJustInfo, SubmissionId);
                        break;
                    }

                if (!doJustInfo)
                    // If no plugin, or if plugin not successfull and toggle then move unknown file
                    if ((pluginResult == null || !pluginResult.IsSuccess) && DoMoveUnknowns)
                    {
                        var uf = UnknownFolder;
                        if (!string.IsNullOrEmpty(uf))
                        {
                            if (!Directory.Exists(uf)) Directory.CreateDirectory(uf);
                            if (!fileInfo.DirectoryName.Equals(UnknownFolder))
                                if (File.Exists(fileInfo.FullName))
                                {
                                    var df = Path.Combine(UnknownFolder,
                                        string.Format("{0}~{1}~{2}", Guid.NewGuid(), fileInfo.Directory.Name,
                                            fileInfo.Name));
                                    Logger.LogDebug("Moving Unknown/Invalid File [{0}] -> [{1}] to UnknownFolder",
                                        fileInfo.FullName, df);
                                    fileInfo.MoveTo(df);
                                }
                        }
                    }

                result = pluginResult;
            }
            catch (PathTooLongException ex)
            {
                Logger.LogError(ex, "Error Processing File. File Name Too Long. Deleting.");
                if (!doJustInfo) fileInfo.Delete();
            }
            catch (Exception ex)
            {
                var willMove = !fileInfo.DirectoryName.Equals(UnknownFolder);
                Logger.LogError(ex,
                    string.Format("Error Processing File [{0}], WillMove [{1}]\n{2}", fileInfo.FullName, willMove,
                        ex.Serialize()));
                string newPath = null;
                try
                {
                    newPath = Path.Combine(UnknownFolder, fileInfo.Directory.Parent.Name, fileInfo.Directory.Name,
                        fileInfo.Name);
                    if (willMove && !doJustInfo)
                    {
                        var directoryPath = Path.GetDirectoryName(newPath);
                        if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
                        fileInfo.MoveTo(newPath);
                    }
                }
                catch (Exception ex1)
                {
                    Logger.LogError(ex1,
                        string.Format("Unable to move file [{0}] to [{1}]", fileInfo.FullName, newPath));
                }
            }

            return result;
        }
    }
}