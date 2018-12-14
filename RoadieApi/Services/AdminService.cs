using Mapster;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadie.Api.Hubs;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Identity;
using Roadie.Library.Models;
using Roadie.Library.Models.Pagination;
using Roadie.Library.Models.Releases;
using Roadie.Library.Models.Statistics;
using Roadie.Library.Models.Users;
using Roadie.Library.Processors;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public class AdminService : ServiceBase, IAdminService
    {
        protected IHubContext<ScanActivityHub> ScanActivityHub { get; }

        public AdminService(IRoadieSettings configuration,
                             IHttpEncoder httpEncoder,
                             IHttpContext httpContext,
                             data.IRoadieDbContext context,
                             ICacheManager cacheManager,
                             ILogger<ArtistService> logger,
                             IHubContext<ScanActivityHub> scanActivityHub
            )
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
            this.ScanActivityHub = scanActivityHub;
        }

        public async Task<OperationResult<bool>> ScanInboundFolder(ApplicationUser user, bool isReadOnly = false)
        {
            var d = new DirectoryInfo(this.Configuration.InboundFolder);
            var dest = new DirectoryInfo(this.Configuration.LibraryFolder);

            var sw = new Stopwatch();
            sw.Start();

            long processedFiles = 0;
            await this.LogAndPublish($"** Processing Folder: [{d.FullName}]");

            long processedFolders = 0;
            var folderProcessor = new FolderProcessor(this.Configuration, this.HttpEncoder, this.Configuration.LibraryFolder, this.DbContext, this.CacheManager, this.Logger);
            foreach (var folder in Directory.EnumerateDirectories(d.FullName).ToArray())
            {
                await folderProcessor.Process(new DirectoryInfo(folder), isReadOnly);
                processedFolders++;
            }
            if (!isReadOnly)
            {
                folderProcessor.DeleteEmptyFolders(d);
            }
            sw.Stop();
            await this.LogAndPublish($"**Completed!Processed Folders[{ processedFolders }], Processed Files[{ processedFiles}] : Elapsed Time[{ sw.Elapsed}]");
            return new OperationResult<bool>
            {
                Data = true,
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        private async Task LogAndPublish(string message, LogLevel level = LogLevel.Trace)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    this.Logger.LogTrace(message);
                    break;
                case LogLevel.Debug:
                    this.Logger.LogDebug(message);
                    break;
                case LogLevel.Information:
                    this.Logger.LogInformation(message);
                    break;
                case LogLevel.Warning:
                    this.Logger.LogWarning(message);
                    break;
                case LogLevel.Critical:
                    this.Logger.LogCritical(message);
                    break;
            }
            await this.ScanActivityHub.Clients.All.SendAsync("SendSystemActivity", message);
        }
    }
}
