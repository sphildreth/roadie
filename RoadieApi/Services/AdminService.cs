using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Api.Hubs;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Enums;
using Roadie.Library.Factories;
using Roadie.Library.Identity;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.FileName;
using Roadie.Library.MetaData.ID3Tags;
using Roadie.Library.MetaData.LastFm;
using Roadie.Library.MetaData.MusicBrainz;
using Roadie.Library.Processors;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using data = Roadie.Library.Data;

namespace Roadie.Api.Services
{
    public class AdminService : ServiceBase, IAdminService
    {
        protected IHubContext<ScanActivityHub> ScanActivityHub { get; }
        private IArtistFactory ArtistFactory { get; }
        private IArtistLookupEngine ArtistLookupEngine { get; }
        private IAudioMetaDataHelper AudioMetaDataHelper { get; }
        private IEventMessageLogger EventMessageLogger { get; }
        private IFileNameHelper FileNameHelper { get; }
        private IID3TagsHelper ID3TagsHelper { get; }
        private IImageFactory ImageFactory { get; }
        private ILabelFactory LabelFactory { get; }
        private ILabelLookupEngine LabelLookupEngine { get; }
        private ILastFmHelper LastFmHelper { get; }

        private ILogger MessageLogger
        {
            get
            {
                return this.EventMessageLogger as ILogger;
            }
        }

        private IMusicBrainzProvider MusicBrainzProvider { get; }
        private IReleaseFactory ReleaseFactory { get; }
        private IReleaseLookupEngine ReleaseLookupEngine { get; }

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

            this.MusicBrainzProvider = new MusicBrainzProvider(configuration, cacheManager, MessageLogger);
            this.LastFmHelper = new LastFmHelper(configuration, cacheManager, MessageLogger);
            this.FileNameHelper = new FileNameHelper(configuration, cacheManager, MessageLogger);
            this.ID3TagsHelper = new ID3TagsHelper(configuration, cacheManager, MessageLogger);

            this.ArtistLookupEngine = new ArtistLookupEngine(configuration, httpEncoder, context, cacheManager, MessageLogger);
            this.LabelLookupEngine = new LabelLookupEngine(configuration, httpEncoder, context, cacheManager, MessageLogger);
            this.ReleaseLookupEngine = new ReleaseLookupEngine(configuration, httpEncoder, context, cacheManager, MessageLogger, this.ArtistLookupEngine, this.LabelLookupEngine);
            this.ImageFactory = new ImageFactory(configuration, httpEncoder, context, cacheManager, MessageLogger, this.ArtistLookupEngine, this.ReleaseLookupEngine);
            this.LabelFactory = new LabelFactory(configuration, httpEncoder, context, cacheManager, MessageLogger, this.ArtistLookupEngine, this.ReleaseLookupEngine);
            this.AudioMetaDataHelper = new AudioMetaDataHelper(configuration, httpEncoder, context, this.MusicBrainzProvider, this.LastFmHelper, cacheManager,
                                                               MessageLogger, this.ArtistLookupEngine, this.ImageFactory, this.FileNameHelper, this.ID3TagsHelper);
            this.ReleaseFactory = new ReleaseFactory(configuration, httpEncoder, context, cacheManager, MessageLogger, this.ArtistLookupEngine, this.LabelFactory, this.AudioMetaDataHelper, this.ReleaseLookupEngine);
            this.ArtistFactory = new ArtistFactory(configuration, httpEncoder, context, cacheManager, MessageLogger, this.ArtistLookupEngine, this.ReleaseFactory, this.ImageFactory, this.ReleaseLookupEngine, this.AudioMetaDataHelper);
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
            var folderProcessor = new FolderProcessor(this.Configuration, this.HttpEncoder, this.Configuration.LibraryFolder, this.DbContext, this.CacheManager, this.MessageLogger, this.ArtistLookupEngine, this.ArtistFactory, this.ReleaseFactory, this.ImageFactory, this.ReleaseLookupEngine, this.AudioMetaDataHelper);

            var newArtists = 0;
            var newReleases = 0;
            var newTracks = 0;
            foreach (var folder in Directory.EnumerateDirectories(d.FullName).ToArray())
            {
                var result = await folderProcessor.Process(new DirectoryInfo(folder), isReadOnly);
                if (result.AdditionalData != null)
                {
                    newArtists += SafeParser.ToNumber<int>(result.AdditionalData["newArtists"]);
                    newReleases += SafeParser.ToNumber<int>(result.AdditionalData["newReleases"]);
                    newTracks += SafeParser.ToNumber<int>(result.AdditionalData["newTracks"]);
                }
                processedFolders++;
            }
            if (!isReadOnly)
            {
                FolderProcessor.DeleteEmptyFolders(d, this.Logger);
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
            this.CacheManager.Clear();
            await this.LogAndPublish($"**Completed!Processed Folders[{ processedFolders }], Processed Files[{ processedFiles}] : Elapsed Time[{ sw.Elapsed}]");
            return new OperationResult<bool>
            {
                Data = true,
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> ScanArtist(ApplicationUser user, Guid artistId, bool isReadOnly = false)
        {
            var sw = new Stopwatch();
            sw.Start();

            var errors = new List<Exception>();
            var artist = this.DbContext.Artists.FirstOrDefault(x => x.RoadieId == artistId);
            if (artist == null)
            {
                await this.LogAndPublish($"ScanArtist Unknown Release [{ artistId}]", LogLevel.Warning);
                return new OperationResult<bool>(true, $"Artist Not Found [{ artistId }]");
            }
            try
            {
                var result = await this.ArtistFactory.ScanArtistReleasesFolders(artist.RoadieId, this.Configuration.LibraryFolder, isReadOnly);
                this.CacheManager.ClearRegion(artist.CacheRegion);
            }
            catch (Exception ex)
            {
                await this.LogAndPublish(ex.ToString(), LogLevel.Error);
                errors.Add(ex);
            }
            sw.Stop();
            this.DbContext.ScanHistories.Add(new data.ScanHistory
            {
                UserId = user.Id,
                ForArtistId = artist.Id,
                NewReleases = this.ReleaseLookupEngine.AddedReleaseIds.Count(),
                NewTracks = this.ReleaseFactory.AddedTrackIds.Count(),
                TimeSpanInSeconds = (int)sw.Elapsed.TotalSeconds
            });
            await this.DbContext.SaveChangesAsync();
            await this.LogAndPublish($"ScanArtist `{artist}`, By User `{user}`", LogLevel.Information);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                AdditionalData = new Dictionary<string, object> { { "artistAverage", artist.Rating } },
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> ScanRelease(ApplicationUser user, Guid releaseId, bool isReadOnly = false)
        {
            var sw = new Stopwatch();
            sw.Start();

            var errors = new List<Exception>();
            var release = this.DbContext.Releases
                                        .Include(x => x.Artist)
                                        .Include(x => x.Labels)
                                        .FirstOrDefault(x => x.RoadieId == releaseId);
            if (release == null)
            {
                await this.LogAndPublish($"ScanRelease Unknown Release [{ releaseId}]", LogLevel.Warning);
                return new OperationResult<bool>(true, $"Release Not Found [{ releaseId }]");
            }
            try
            {
                var result = await this.ReleaseFactory.ScanReleaseFolder(release.RoadieId, this.Configuration.LibraryFolder, isReadOnly, release);
                this.CacheManager.ClearRegion(release.CacheRegion);
            }
            catch (Exception ex)
            {
                await this.LogAndPublish(ex.ToString(), LogLevel.Error);
                errors.Add(ex);
            }
            sw.Stop();
            this.DbContext.ScanHistories.Add(new data.ScanHistory
            {
                UserId = user.Id,
                ForReleaseId = release.Id,
                NewTracks = this.ReleaseFactory.AddedTrackIds.Count(),
                TimeSpanInSeconds = (int)sw.Elapsed.TotalSeconds
            });
            await this.DbContext.SaveChangesAsync();
            await this.LogAndPublish($"ScanRelease `{release}`, By User `{user}`", LogLevel.Information);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteRelease(ApplicationUser user, Guid releaseId, bool? doDeleteFiles)
        {
            var sw = new Stopwatch();
            sw.Start();

            var errors = new List<Exception>();

            var release = this.DbContext.Releases.Include(x => x.Artist).FirstOrDefault(x => x.RoadieId == releaseId);
            try
            {
                if (release == null)
                {
                    await this.LogAndPublish($"DeleteRelease Unknown Release [{ releaseId}]", LogLevel.Warning);
                    return new OperationResult<bool>(true, $"Release Not Found [{ releaseId }]");
                }
                await this.ReleaseFactory.Delete(release, doDeleteFiles ?? false);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex);
                await this.LogAndPublish("Error deleting release.");
                errors.Add(ex);
            }
            sw.Stop();
            await this.LogAndPublish($"DeleteRelease `{ release }`, By User `{ user}`", LogLevel.Information);
            this.CacheManager.Clear();
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data =true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };

        }

        public async Task<OperationResult<bool>> DeleteArtist(ApplicationUser user, Guid artistId)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var artist = this.DbContext.Artists.FirstOrDefault(x => x.RoadieId == artistId);
            if (artist == null)
            {
                await this.LogAndPublish($"DeleteArtist Unknown Artist [{ artistId}]", LogLevel.Warning);
                return new OperationResult<bool>(true, $"Artist Not Found [{ artistId }]");
            }
            try
            {
                var result = await this.ArtistFactory.Delete(artist);
                if (!result.IsSuccess)
                {
                    return new OperationResult<bool>
                    {
                        Errors = result.Errors
                    };
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex);
                await this.LogAndPublish("Error deleting artist.");
                errors.Add(ex);
            }
            sw.Stop();
            await this.LogAndPublish($"DeleteArtist `{ artist }`, By User `{user }`", LogLevel.Information);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data= true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        private void EventMessageLogger_Messages(object sender, EventMessage e)
        {
            Task.WaitAll(this.LogAndPublish(e.Message, e.Level));
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