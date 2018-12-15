using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Extensions;
using Roadie.Library.Factories;
using Roadie.Library.FilePlugins;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.Processors
{
    public sealed class FileProcessor : ProcessorBase
    {
        private IEnumerable<IFilePlugin> _plugins = null;

        public IEnumerable<IFilePlugin> Plugins
        {
            get
            {
                if (this._plugins == null)
                {
                    var plugins = new List<IFilePlugin>();
                    try
                    {
                        var type = typeof(IFilePlugin);
                        var types = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(s => s.GetTypes())
                            .Where(p => type.IsAssignableFrom(p));
                        foreach (Type t in types)
                        {
                            if (t.GetInterface("IFilePlugin") != null && !t.IsAbstract && !t.IsInterface)
                            {
                                IFilePlugin plugin = Activator.CreateInstance(t, new object[] { this.Configuration, this.HttpEncoder, this.ArtistFactory, this.ReleaseFactory, this.ImageFactory, this.CacheManager, this.Logger }) as IFilePlugin;
                                plugins.Add(plugin);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Logger.LogError(ex);
                    }
                    this._plugins = plugins.ToArray();
                }
                return this._plugins;
            }
        }

        public FileProcessor(IRoadieSettings configuration, IHttpEncoder httpEncoder, string destinationRoot, IRoadieDbContext context, ICacheManager cacheManager, ILogger logger, IArtistLookupEngine artistLookupEngine, IArtistFactory artistFactory, IReleaseFactory releaseFactory, IImageFactory imageFactory)
            : base(configuration, httpEncoder, destinationRoot, context, cacheManager, logger, artistLookupEngine, artistFactory, releaseFactory, imageFactory)
        {
        }

        public static string DetermineFileType(System.IO.FileInfo fileinfo)
        {
            string r = MimeMapping.MimeUtility.GetMimeMapping(fileinfo.FullName);
            if (r.Equals("application/octet-stream"))
            {
                if (fileinfo.Extension.Equals(".cue"))
                {
                    r = "audio/r-cue";
                }
                if (fileinfo.Extension.Equals(".mp4") || fileinfo.Extension.Equals(".m4a"))
                {
                    r = "audio/mp4";
                }
            }
            Trace.WriteLine(string.Format("FileType [{0}] For File [{1}]", r, fileinfo.FullName));
            return r;
        }

        public async Task<OperationResult<bool>> Process(string filename, bool doJustInfo = false)
        {
            return await this.Process(new FileInfo(filename), doJustInfo);
        }

        public async Task<OperationResult<bool>> Process(FileInfo fileInfo, bool doJustInfo = false)
        {
            var result = new OperationResult<bool>();

            try
            {
                // Determine what type of file this is
                var fileType = FileProcessor.DetermineFileType(fileInfo);

                OperationResult<bool> pluginResult = null;
                foreach (var p in this.Plugins)
                {
                    // See if there is a plugin
                    if (p.HandlesTypes.Contains(fileType))
                    {
                        pluginResult = await p.Process(this.DestinationRoot, fileInfo, doJustInfo, this.SubmissionId);
                        break;
                    }
                }

                if (!doJustInfo)
                {
                    // If no plugin, or if plugin not successfull and toggle then move unknown file
                    if ((pluginResult == null || !pluginResult.IsSuccess) && this.DoMoveUnknowns)
                    {
                        var uf = this.UnknownFolder;
                        if (!string.IsNullOrEmpty(uf))
                        {
                            if (!Directory.Exists(uf))
                            {
                                Directory.CreateDirectory(uf);
                            }
                            if (!fileInfo.DirectoryName.Equals(this.UnknownFolder))
                            {
                                if (File.Exists(fileInfo.FullName))
                                {
                                    var df = Path.Combine(this.UnknownFolder, string.Format("{0}~{1}~{2}", Guid.NewGuid(), fileInfo.Directory.Name, fileInfo.Name));
                                    this.Logger.LogDebug("Moving Unknown/Invalid File [{0}] -> [{1}] to UnknownFolder", fileInfo.FullName, df);
                                    fileInfo.MoveTo(df);
                                }
                            }
                        }
                    }
                }
                result = pluginResult;
            }
            catch (System.IO.PathTooLongException ex)
            {
                this.Logger.LogError(ex, string.Format("Error Processing File. File Name Too Long. Deleting."));
                if (!doJustInfo)
                {
                    fileInfo.Delete();
                }
            }
            catch (Exception ex)
            {
                var willMove = !fileInfo.DirectoryName.Equals(this.UnknownFolder);
                this.Logger.LogError(ex, string.Format("Error Processing File [{0}], WillMove [{1}]\n{2}", fileInfo.FullName, willMove, ex.Serialize()));
                string newPath = null;
                try
                {
                    newPath = Path.Combine(this.UnknownFolder, fileInfo.Directory.Parent.Name, fileInfo.Directory.Name, fileInfo.Name);
                    if (willMove && !doJustInfo)
                    {
                        var directoryPath = Path.GetDirectoryName(newPath);
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }
                        fileInfo.MoveTo(newPath);
                    }
                }
                catch (Exception ex1)
                {
                    this.Logger.LogError(ex1, string.Format("Unable to move file [{0}] to [{1}]", fileInfo.FullName, newPath));
                }
            }
            return result;
        }
    }
}