using Microsoft.Extensions.Logging;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Extensions;
using Roadie.Library.FilePlugins;
using Roadie.Library.Identity;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.Processors;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace Roadie.Api.Services
{
    public class FileDirectoryProcessorService : ServiceBase, IFileDirectoryProcessorService
    {

        private IArtistLookupEngine ArtistLookupEngine { get; }

        private IAudioMetaDataHelper AudioMetaDataHelper { get; }

        private IFileProcessor FileProcessor { get; }
        private IReleaseLookupEngine ReleaseLookupEngine { get; }
        private IReleaseService ReleaseService { get; }

        private List<int> _addedArtistIds = new List<int>();
        private List<int> _addedReleaseIds = new List<int>();
        private List<int> _addedTrackIds = new List<int>();

        public IEnumerable<int> AddedArtistIds => _addedArtistIds.Distinct();
        public IEnumerable<int> AddedReleaseIds => _addedReleaseIds.Distinct();
        public IEnumerable<int> AddedTrackIds => _addedTrackIds.Distinct();

        public int? ProcessLimit { get; set; }

        public FileDirectoryProcessorService(IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            IRoadieDbContext context,
            ICacheManager cacheManager,
            ILogger<FileDirectoryProcessorService> logger,
            IArtistLookupEngine artistLookupEngine,
            IFileProcessor fileProcessor,
            IReleaseLookupEngine releaseLookupEngine,
            IAudioMetaDataHelper audioMetaDataHelper,
            IReleaseService releaseService)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
            ArtistLookupEngine = artistLookupEngine;
            AudioMetaDataHelper = audioMetaDataHelper;
            ReleaseLookupEngine = releaseLookupEngine;
            ReleaseService = releaseService;
            FileProcessor = fileProcessor;
        }

        /// <summary>
        ///     Perform any operations to the given folder and the plugin results after processing
        /// </summary>
        private async Task<bool> PostProcessFolderAsync(User user, DirectoryInfo inboundFolder, IEnumerable<PluginResultInfo> pluginResults, bool doJustInfo, bool doDeleteFiles = true)
        {
            SimpleContract.Requires<ArgumentNullException>(inboundFolder != null, "Invalid InboundFolder");
            if (pluginResults != null)
            {
                foreach (var releasesInfo in pluginResults.GroupBy(x => x.ReleaseId).Select(x => x.First()))
                {
                    await ReleaseService.ScanReleaseFolderAsync(user, releasesInfo.ReleaseId, doJustInfo).ConfigureAwait(false);
                    _addedTrackIds.AddRange(ReleaseService.AddedTrackIds);
                }
            }
            if (!doJustInfo && doDeleteFiles)
            {
                var fileExtensionsToDelete = Configuration.FileExtensionsToDelete ?? new string[0];
                if (fileExtensionsToDelete.Any())
                {
                    foreach (var fileInFolder in inboundFolder.GetFiles("*.*", SearchOption.AllDirectories))
                    {
                        if (fileExtensionsToDelete.Any(x => x.Equals(fileInFolder.Extension, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (!doJustInfo)
                            {
                                fileInFolder.Delete();
                                Logger.LogTrace("Deleted File [{0}], Was found in in FileExtensionsToDelete",
                                    fileInFolder.Name);
                            }
                        }
                    }
                }

                DeleteEmptyFolders(inboundFolder, Logger);
            }

            return true;
        }

        /// <summary>
        ///     Perform any operations to the given folder before processing
        /// </summary>
        private Task<bool> PreProcessFolderAsync(DirectoryInfo inboundFolder, bool doJustInfo = false)
        {
            return Task.FromResult(true);
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
                logger.LogError(ex, $"Error Deleting Empty Folder [{processingFolder.FullName}] Error [{ex.Serialize()}]");
            }
            return result;
        }

        public async Task<OperationResult<bool>> ProcessAsync(User user, DirectoryInfo folder, bool doJustInfo, int? submissionId = null, bool doDeleteFiles = true)
        {
            var sw = new Stopwatch();
            sw.Start();
            await PreProcessFolderAsync(folder, doJustInfo).ConfigureAwait(false);
            var processedFiles = 0;
            var pluginResultInfos = new List<PluginResultInfo>();
            var errors = new List<string>();

            _addedArtistIds.Clear();
            _addedReleaseIds.Clear();
            _addedTrackIds.Clear();

            FileProcessor.SubmissionId = submissionId;

            foreach (var file in Directory.EnumerateFiles(folder.FullName, "*.*", SearchOption.AllDirectories) .ToArray())
            {
                var operation = await FileProcessor.Process(file, doJustInfo).ConfigureAwait(false);
                if (operation != null && operation.AdditionalData != null &&
                    operation.AdditionalData.ContainsKey(PluginResultInfo.AdditionalDataKeyPluginResultInfo))
                {
                    pluginResultInfos.Add(operation.AdditionalData[PluginResultInfo.AdditionalDataKeyPluginResultInfo] as PluginResultInfo);
                    processedFiles++;
                }
                if (ProcessLimit.HasValue && processedFiles > ProcessLimit.Value)
                {
                    break;
                }
            }

            await PostProcessFolderAsync(user, folder, pluginResultInfos, doJustInfo, doDeleteFiles).ConfigureAwait(false);
            sw.Stop();
            _addedArtistIds.AddRange(ArtistLookupEngine.AddedArtistIds);
            _addedReleaseIds.AddRange(ReleaseLookupEngine.AddedReleaseIds);
            _addedTrackIds.AddRange(ReleaseLookupEngine.AddedTrackIds);
            Logger.LogInformation("Completed! Processed Folder [{0}]: Processed Files [{1}] : Elapsed Time [{2}]", folder.FullName, processedFiles, sw.Elapsed);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                AdditionalData = new Dictionary<string, object> { { "ProcessedFiles", processedFiles } },
                OperationTime = sw.ElapsedMilliseconds
            };
        }
    }
}