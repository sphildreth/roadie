using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Extensions;
using Roadie.Library.Factories;
using Roadie.Library.FilePlugins;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.Processors
{
    public sealed class FolderProcessor : ProcessorBase
    {
        public int? ProcessLimit { get; set; }

        private FileProcessor FileProcessor { get; }

        public FolderProcessor(IRoadieSettings configuration, IHttpEncoder httpEncoder, string destinationRoot,
                            IRoadieDbContext context, ICacheManager cacheManager, ILogger logger,
            IArtistLookupEngine artistLookupEngine, IArtistFactory artistFactory, IReleaseFactory releaseFactory,
            IImageFactory imageFactory, IReleaseLookupEngine releaseLookupEngine,
            IAudioMetaDataHelper audioMetaDataHelper)
            : base(configuration, httpEncoder, destinationRoot, context, cacheManager, logger, artistLookupEngine,
                artistFactory, releaseFactory, imageFactory, releaseLookupEngine, audioMetaDataHelper)
        {
            SimpleContract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(destinationRoot),
                "Invalid Destination Folder");
            FileProcessor = new FileProcessor(configuration, httpEncoder, destinationRoot, context, cacheManager,
                logger, artistLookupEngine, artistFactory, releaseFactory, imageFactory, releaseLookupEngine,
                audioMetaDataHelper);
        }

        public static OperationResult<bool> DeleteEmptyFolders(DirectoryInfo processingFolder, ILogger logger)
        {
            var result = new OperationResult<bool>();
            try
            {
                result.IsSuccess = FolderPathHelper.DeleteEmptyFolders(processingFolder);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    string.Format("Error Deleting Empty Folder [{0}] Error [{1}]", processingFolder.FullName,
                        ex.Serialize()));
            }

            return result;
        }

        public async Task<OperationResult<bool>> Process(DirectoryInfo folder, bool doJustInfo,
            int? submissionId = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            await PreProcessFolder(folder, doJustInfo);
            var processedFiles = 0;
            var pluginResultInfos = new List<PluginResultInfo>();
            var errors = new List<string>();

            FileProcessor.SubmissionId = submissionId;

            foreach (var file in Directory.EnumerateFiles(folder.FullName, "*.*", SearchOption.AllDirectories)
                .ToArray())
            {
                var operation = await FileProcessor.Process(file, doJustInfo);
                if (operation != null && operation.AdditionalData != null &&
                    operation.AdditionalData.ContainsKey(PluginResultInfo.AdditionalDataKeyPluginResultInfo))
                    pluginResultInfos.Add(
                        operation.AdditionalData[
                            PluginResultInfo.AdditionalDataKeyPluginResultInfo] as PluginResultInfo);
                processedFiles++;
                if (ProcessLimit.HasValue && processedFiles > ProcessLimit.Value) break;
            }

            await PostProcessFolder(folder, pluginResultInfos, doJustInfo);
            sw.Stop();
            Logger.LogInformation("** Completed! Processed Folder [{0}]: Processed Files [{1}] : Elapsed Time [{2}]",
                folder.FullName, processedFiles, sw.Elapsed);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                AdditionalData = new Dictionary<string, object>
                {
                    {"processedFiles", processedFiles},
                    {"newArtists", ArtistLookupEngine.AddedArtistIds.Count()},
                    {"newReleases", ReleaseLookupEngine.AddedReleaseIds.Count()},
                    {"newTracks", ReleaseFactory.AddedTrackIds.Count()}
                },
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        /// <summary>
        ///     Perform any operations to the given folder and the plugin results after processing
        /// </summary>
        private async Task<bool> PostProcessFolder(DirectoryInfo inboundFolder,
            IEnumerable<PluginResultInfo> pluginResults, bool doJustInfo)
        {
            SimpleContract.Requires<ArgumentNullException>(inboundFolder != null, "Invalid InboundFolder");
            if (pluginResults != null)
                foreach (var releasesInfo in pluginResults.GroupBy(x => x.ReleaseId).Select(x => x.First()))
                    await ReleaseFactory.ScanReleaseFolder(releasesInfo.ReleaseId, DestinationRoot, doJustInfo);
            if (!doJustInfo)
            {
                var fileExtensionsToDelete = Configuration.FileExtensionsToDelete ?? new string[0];
                if (fileExtensionsToDelete.Any())
                    foreach (var fileInFolder in inboundFolder.GetFiles("*.*", SearchOption.AllDirectories))
                        if (fileExtensionsToDelete.Any(x =>
                            x.Equals(fileInFolder.Extension, StringComparison.OrdinalIgnoreCase)))
                            if (!doJustInfo)
                            {
                                fileInFolder.Delete();
                                Logger.LogInformation("x Deleted File [{0}], Was foud in in FileExtensionsToDelete",
                                    fileInFolder.Name);
                            }

                DeleteEmptyFolders(inboundFolder, Logger);
            }

            return true;
        }

        /// <summary>
        ///     Perform any operations to the given folder before processing
        /// </summary>
        private Task<bool> PreProcessFolder(DirectoryInfo inboundFolder, bool doJustInfo = false)
        {
            return Task.FromResult(true);
        }
    }
}