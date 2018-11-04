using Roadie.Library.Caching;
using Roadie.Library.Extensions;
using Roadie.Library.FilePlugins;
using Roadie.Library.Utility;
using Roadie.Library.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;

namespace Roadie.Library.Processors
{
    public sealed class FolderProcessor : ProcessorBase
    {
        private readonly FileProcessor _fileProcessor;
        private FileProcessor FileProcessor
        {
            get
            {
                return this._fileProcessor;
            }
        }
        public int? ProcessLimit { get; set; }

        public FolderProcessor(IConfiguration configuration, IHttpEncoder httpEncoder, string destinationRoot, IRoadieDbContext context, ICacheManager cacheManager, ILogger loggingService) 
            : base(configuration, httpEncoder, destinationRoot, context, cacheManager, loggingService)
        {
            SimpleContract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(destinationRoot), "Invalid Destination Folder");
            this._fileProcessor = new FileProcessor(configuration, httpEncoder, destinationRoot, context, cacheManager, loggingService);
        }

        public async Task<OperationResult<bool>> Process(DirectoryInfo inboundFolder, bool doJustInfo, int? submissionId = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            this.PrePrecessFolder(inboundFolder, doJustInfo);
            int processedFiles = 0;
            var pluginResultInfos = new List<PluginResultInfo>();
            var errors = new List<string>();
            this.FileProcessor.SubmissionId = submissionId;
            foreach (var file in Directory.EnumerateFiles(inboundFolder.FullName, "*.*", SearchOption.AllDirectories).ToArray())
            {
                var operation = await this.FileProcessor.Process(file, doJustInfo);
                if(operation != null && operation.AdditionalData != null && operation.AdditionalData.ContainsKey(PluginResultInfo.AdditionalDataKeyPluginResultInfo))
                {
                    pluginResultInfos.Add(operation.AdditionalData[PluginResultInfo.AdditionalDataKeyPluginResultInfo] as PluginResultInfo);
                }
                if(operation == null)
                {
                    var fileExtensionsToDelete = this.Configuration.GetValue<string[]>("FileExtensionsToDelete", new string[0]);
                    if (fileExtensionsToDelete.Any(x => x.Equals(Path.GetExtension(file), StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!doJustInfo)
                        {
                            if (!Path.GetFileNameWithoutExtension(file).ToLower().Equals("cover"))
                            {
                                File.Delete(file);
                                this.LoggingService.Info("x Deleted File [{0}], Was foud in in FileExtensionsToDelete", file);
                            }
                        }
                    }
                }
                processedFiles++;
                if (this.ProcessLimit.HasValue && processedFiles > this.ProcessLimit.Value)
                {
                    break;
                }
            }
            await this.PostProcessFolder(inboundFolder, pluginResultInfos, doJustInfo);
            sw.Stop();
            this.LoggingService.Info("** Completed! Processed Folder [{0}]: Processed Files [{1}] : Elapsed Time [{2}]", inboundFolder.FullName.ToString(), processedFiles, sw.Elapsed);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                AdditionalData = new Dictionary<string, object> {
                    { "processedFiles", processedFiles },
                    { "newArtists",  this.ArtistFactory.AddedArtistIds.Count() },
                    { "newReleases", this.ReleaseFactory.AddedReleaseIds.Count() },
                    { "newTracks",  this.ReleaseFactory.AddedTrackIds.Count() }
                },

                OperationTime = sw.ElapsedMilliseconds
            };
        }

        /// <summary>
        /// Perform any operations to the given folder before processing
        /// </summary>
        private bool PrePrecessFolder(DirectoryInfo inboundFolder, bool doJustInfo = false)
        {
            // If Folder name starts with "~" then remove the tilde and set all files in the folder artist to the folder name
            if (this.Configuration.GetValue<bool>("Processing:DoFolderArtistNameSet", true) && inboundFolder.Name.StartsWith("~"))
            {
                var artist = inboundFolder.Name.Replace("~", "");
                this.LoggingService.Info("Setting Folder File Tags To [{0}]", artist);
                if (!doJustInfo)
                {
                    foreach (var file in inboundFolder.GetFiles("*.*", SearchOption.AllDirectories))
                    {
                        var extension = file.Extension.ToLower();
                        if (extension.Equals(".mp3") || extension.Equals(".flac"))
                        {
                            var tagFile = TagLib.File.Create(file.FullName);
                            tagFile.Tag.Performers = null;
                            tagFile.Tag.Performers = new[] { artist };
                            tagFile.Save();
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Perform any operations to the given folder and the plugin results after processing
        /// </summary>
        private async Task<bool> PostProcessFolder(DirectoryInfo inboundFolder, IEnumerable<PluginResultInfo> pluginResults, bool doJustInfo)
        {
            SimpleContract.Requires<ArgumentNullException>(inboundFolder != null, "Invalid InboundFolder");
            if (!doJustInfo)
            {
                this.DeleteEmptyFolders(inboundFolder);
            }
            if (pluginResults != null)
            {
                //await Task.Run(() => Parallel.ForEach(pluginResults.GroupBy(x => x.ReleaseId).Select(x => x.First()), async releasesInfo =>
                //{
                //    await this.ReleaseFactory.ScanReleaseFolder(releasesInfo.ReleaseId, this.DestinationRoot, doJustInfo);
                //}));

                foreach (var releasesInfo in pluginResults.GroupBy(x => x.ReleaseId).Select(x => x.First()))
                {
                    await this.ReleaseFactory.ScanReleaseFolder(releasesInfo.ReleaseId, this.DestinationRoot, doJustInfo);
                }
            }
            return true;
        }

        public OperationResult<bool> DeleteEmptyFolders(DirectoryInfo processingFolder)
        {
            var result = new OperationResult<bool>();
            try
            {
                result.IsSuccess = FolderPathHelper.DeleteEmptyFolders(processingFolder);
            }
            catch (Exception ex)
            {
                this.LoggingService.Error(ex, string.Format("Error Deleting Empty Folder [{0}] Error [{1}]", processingFolder.FullName, ex.Serialize()));
            }
            return result;
        }

    }
}
