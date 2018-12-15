using Mapster;
using Microsoft.AspNetCore.Identity;
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
        private IEventMessageLogger EventMessageLogger { get; }

        private ILogger MessageLogger
        {
            get
            {
                return this.EventMessageLogger as ILogger;
            }
        }

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
            this.EventMessageLogger = new EventMessageLogger();
            this.EventMessageLogger.Messages += EventMessageLogger_Messages;
        }

        private void EventMessageLogger_Messages(object sender, EventMessage e)
        {
            Task.WaitAll(this.LogAndPublish(e.Message, e.Level));
        }

        /// <summary>
        /// This is a very simple way to seed the database or setup configuration when the first (who becomes "Admin") user registers
        /// </summary>
        public async Task<OperationResult<bool>> DoInitialSetup(ApplicationUser user, UserManager<ApplicationUser> userManager)
        {
            var sw = new Stopwatch();
            sw.Start();

            // Create user roles
            this.DbContext.UserRoles.Add(new ApplicationRole
            {
                Name = "Admin",
                Description = "Users with Administrative (full) access",
                NormalizedName = "ADMIN"
            });
            this.DbContext.UserRoles.Add(new ApplicationRole
            {
                Name = "Editor",
                Description = "Users who have Edit Permissions",
                NormalizedName = "EDITOR"
            });
            await this.DbContext.SaveChangesAsync();

            // Add given user to Admin role
            await userManager.AddToRoleAsync(user, "Admin");

            // Create special system artists of 'Sound Tracks' and 'Various Artists'
            this.DbContext.Artists.Add(new data.Artist
            {
                AlternateNames = "Sound Track|Film Sound Track|Film Sound Tracks|Les Sound Track|Motion Picture Soundtrack|Original Motion Picture SoundTrack|Original Motion Picture SoundTracks|Original Cast Album|Original Soundtrack|Soundtracks|SoundTrack|soundtracks|Original Cast|Original Cast Soundtrack|Motion Picture Cast Recording|Cast Recording",
                ArtistType = "Meta",
                BioContext = "A soundtrack, also written sound track, can be recorded music accompanying and synchronized to the images of a motion picture, book, television program or video game; a commercially released soundtrack album of music as featured in the soundtrack of a film or TV show; or the physical area of a film that contains the synchronized recorded sound.",
                Name = "Sound Tracks",
                SortName = "Sound Tracks",
                Status = Statuses.Ok,
                Tags = "movie and television soundtracks|video game soundtracks|book soundstracks|composite|compilations",
                URLs = "https://en.wikipedia.org/wiki/Soundtrack"
            });
            this.DbContext.Artists.Add(new data.Artist
            {
                AlternateNames = "Various Artists|Various BNB artist|variousartist|va",
                ArtistType = "Meta",
                BioContext = "Songs included on a compilation album may be previously released or unreleased, usually from several separate recordings by either one or several performers. If by one artist, then generally the tracks were not originally intended for release together as a single work, but may be collected together as a greatest hits album or box set. If from several performers, there may be a theme, topic, or genre which links the tracks, or they may have been intended for release as a single work—such as a tribute album. When the tracks are by the same recording artist, the album may be referred to as a retrospective album or an anthology. Compilation albums may employ traditional product bundling strategies",
                Name = "Various Artists",
                SortName = "Various Artist",
                Status = Statuses.Ok,
                Tags = "compilations|various",                
                URLs = "https://en.wikipedia.org/wiki/Compilation_album"
            });
            await this.DbContext.SaveChangesAsync();

            return new OperationResult<bool>
            {
                Data = true,
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            };
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
            var folderProcessor = new FolderProcessor(this.Configuration, this.HttpEncoder, this.Configuration.LibraryFolder, this.DbContext, this.CacheManager, this.MessageLogger);

            var newArtists = 0;
            var newReleases = 0;
            var newTracks = 0;
            foreach (var folder in Directory.EnumerateDirectories(d.FullName).ToArray())
            {
                var result = await folderProcessor.Process(new DirectoryInfo(folder), isReadOnly);
                if(result.AdditionalData != null)
                {
                    newArtists += SafeParser.ToNumber<int>(result.AdditionalData["newArtists"]);
                    newReleases += SafeParser.ToNumber<int>(result.AdditionalData["newReleases"]);
                    newTracks += SafeParser.ToNumber<int>(result.AdditionalData["newTracks"]);
                }
                processedFolders++;
            }
            if (!isReadOnly)
            {
                folderProcessor.DeleteEmptyFolders(d);
            }
            sw.Stop();
            this.DbContext.ScanHistories.Add(new data.ScanHistory
            {
                UserId = user.Id,
                NewArtists = newArtists,
                NewReleases = newReleases,
                NewTracks = newTracks,
                TimeSpanInSeconds = (int)sw.Elapsed.TotalSeconds                
            });
            await this.DbContext.SaveChangesAsync();
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
