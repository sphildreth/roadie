using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roadie.Api.Hubs;
using Roadie.Library;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Identity;
using Roadie.Library.Imaging;
using Roadie.Library.Models.Collections;
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
        private IArtistLookupEngine ArtistLookupEngine { get; }

        private IArtistService ArtistService { get; }
        private IBookmarkService BookmarkService { get; }

        private IEventMessageLogger EventMessageLogger { get; }

        private IFileDirectoryProcessorService FileDirectoryProcessorService { get; }

        private IGenreService GenreService { get; }

        private ILabelService LabelService { get; }

        private IReleaseLookupEngine ReleaseLookupEngine { get; }

        private IReleaseService ReleaseService { get; }

        protected IHubContext<ScanActivityHub> ScanActivityHub { get; }

        public AdminService(
            IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IHttpContext httpContext,
            IRoadieDbContext context,
            ICacheManager cacheManager,
            ILogger<ArtistService> logger,
            IHubContext<ScanActivityHub> scanActivityHub,
            IFileDirectoryProcessorService fileDirectoryProcessorService,
            IArtistService artistService,
            IReleaseService releaseService,
            IArtistLookupEngine artistLookupEngine,
            IReleaseLookupEngine releaseLookupEngine,
            ILabelService labelService,
            IGenreService genreService,
            IBookmarkService bookmarkService)
            : base(configuration, httpEncoder, context, cacheManager, logger, httpContext)
        {
            ScanActivityHub = scanActivityHub;
            EventMessageLogger = new EventMessageLogger<AdminService>();
            EventMessageLogger.Messages += EventMessageLogger_Messages;

            ArtistService = artistService;
            ReleaseService = releaseService;
            LabelService = labelService;
            GenreService = genreService;
            ArtistLookupEngine = artistLookupEngine;
            ReleaseLookupEngine = releaseLookupEngine;
            FileDirectoryProcessorService = fileDirectoryProcessorService;
            BookmarkService = bookmarkService;
        }

        private void EventMessageLogger_Messages(object sender, EventMessage e) => Task.WaitAll(LogAndPublishAsync(e.Message, e.Level));

        private Task LogAndPublishAsync(string message, LogLevel level = LogLevel.Trace)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    Logger.LogTrace(message);
                    break;

                case LogLevel.Debug:
                    Logger.LogDebug(message);
                    break;

                case LogLevel.Information:
                    Logger.LogInformation(message);
                    break;

                case LogLevel.Warning:
                    Logger.LogWarning(message);
                    break;

                case LogLevel.Critical:
                    Logger.LogCritical(message);
                    break;
            }
            return ScanActivityHub.Clients.All.SendAsync("SendSystemActivityAsync", level, message);
        }

        private async Task<OperationResult<bool>> ScanFolderAsync(User user, DirectoryInfo d, bool isReadOnly, bool doDeleteFiles = true)
        {
            var sw = new Stopwatch();
            sw.Start();

            long processedFiles = 0;
            await LogAndPublishAsync($"*\\ Processing Folder: [{d.FullName}]").ConfigureAwait(false);

            long processedFolders = 0;
            foreach (var folder in Directory.EnumerateDirectories(d.FullName).ToArray())
            {
                var directoryProcessResult = await FileDirectoryProcessorService.ProcessAsync(user: user,
                                                                                         folder: new DirectoryInfo(folder),
                                                                                         doJustInfo: isReadOnly,
                                                                                         doDeleteFiles: doDeleteFiles).ConfigureAwait(false);
                processedFolders++;
                processedFiles += SafeParser.ToNumber<int>(directoryProcessResult.AdditionalData["ProcessedFiles"]);
                await LogAndPublishAsync($"*+ Processing Folder: [{folder}]").ConfigureAwait(false);
            }
            CacheManager.Clear();
            if (!isReadOnly)
            {
                Services.FileDirectoryProcessorService.DeleteEmptyFolders(d, Logger);
            }
            sw.Stop();
            var newScanHistory = new data.ScanHistory
            {
                UserId = user.Id,
                NewArtists = FileDirectoryProcessorService.AddedArtistIds.Count(),
                NewReleases = FileDirectoryProcessorService.AddedReleaseIds.Count(),
                NewTracks = FileDirectoryProcessorService.AddedTrackIds.Count(),
                TimeSpanInSeconds = (int)sw.Elapsed.TotalSeconds
            };
            DbContext.ScanHistories.Add(newScanHistory);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            await LogAndPublishAsync($"*/ Completed! Processed Folders [{processedFolders}], Processed Files [{processedFiles}], New Artists [{ newScanHistory.NewArtists }], New Releases [{ newScanHistory.NewReleases }], New Tracks [{ newScanHistory.NewTracks }] : Elapsed Time [{sw.Elapsed}]").ConfigureAwait(false);
            return new OperationResult<bool>
            {
                Data = true,
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> DeleteArtistAsync(User user, Guid artistId, bool deleteFolder)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var artist = await DbContext.Artists.FirstOrDefaultAsync(x => x.RoadieId == artistId).ConfigureAwait(false);
            if (artist == null)
            {
                await LogAndPublishAsync($"DeleteArtist Unknown Artist [{artistId}]", LogLevel.Warning).ConfigureAwait(false);
                return new OperationResult<bool>(true, $"Artist Not Found [{artistId}]");
            }

            try
            {
                var result = await ArtistService.DeleteAsync(user, artist, deleteFolder).ConfigureAwait(false);
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
                Logger.LogError(ex);
                await LogAndPublishAsync("Error deleting artist.").ConfigureAwait(false);
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublishAsync($"DeleteArtist `{artist}`, By User `{user}`", LogLevel.Information).ConfigureAwait(false);
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteArtistReleasesAsync(User user, Guid artistId, bool doDeleteFiles = false)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var artist = DbContext.Artists.FirstOrDefault(x => x.RoadieId == artistId);
            if (artist == null)
            {
                await LogAndPublishAsync($"DeleteArtistReleases Unknown Artist [{artistId}]", LogLevel.Warning).ConfigureAwait(false);
                return new OperationResult<bool>(true, $"Artist Not Found [{artistId}]");
            }

            try
            {
                await ReleaseService.DeleteReleasesAsync(user, DbContext.Releases.Where(x => x.ArtistId == artist.Id).Select(x => x.RoadieId).ToArray(), doDeleteFiles).ConfigureAwait(false);
                await DbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await LogAndPublishAsync("Error deleting artist.").ConfigureAwait(false);
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublishAsync($"DeleteArtistReleases `{artist}`, By User `{user}`", LogLevel.Information).ConfigureAwait(false);
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteArtistSecondaryImageAsync(User user, Guid artistId, int index)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var artist = DbContext.Artists.FirstOrDefault(x => x.RoadieId == artistId);
            if (artist == null)
            {
                await LogAndPublishAsync($"DeleteArtistSecondaryImage Unknown Artist [{artistId}]", LogLevel.Warning).ConfigureAwait(false);
                return new OperationResult<bool>(true, $"Artist Not Found [{artistId}]");
            }

            try
            {
                var artistFolder = artist.ArtistFileFolder(Configuration);
                var artistImagesInFolder = ImageHelper.FindImageTypeInDirectory(new DirectoryInfo(artistFolder), ImageType.ArtistSecondary, SearchOption.TopDirectoryOnly);
                var artistImageFilename = artistImagesInFolder.Skip(index).FirstOrDefault();
                if (artistImageFilename.Exists)
                {
                    artistImageFilename.Delete();
                }
                CacheManager.ClearRegion(artist.CacheRegion);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await LogAndPublishAsync("Error deleting artist secondary image.").ConfigureAwait(false);
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublishAsync($"DeleteArtistSecondaryImage `{artist}` Index [{index}], By User `{user}`", LogLevel.Information).ConfigureAwait(false);
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteGenreAsync(User user, Guid genreId)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var genre = DbContext.Genres.FirstOrDefault(x => x.RoadieId == genreId);
            if (genre == null)
            {
                await LogAndPublishAsync($"DeleteLabel Unknown Genre [{genreId}]", LogLevel.Warning).ConfigureAwait(false);
                return new OperationResult<bool>(true, $"Genre Not Found [{genreId}]");
            }

            try
            {
                await GenreService.DeleteAsync(user, genreId).ConfigureAwait(false);
                CacheManager.ClearRegion(genre.CacheRegion);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await LogAndPublishAsync("Error deleting Genre.").ConfigureAwait(false);
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublishAsync($"DeleteGenre `{genre}`, By User `{user}`", LogLevel.Information).ConfigureAwait(false);
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteLabelAsync(User user, Guid labelId)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var label = DbContext.Labels.FirstOrDefault(x => x.RoadieId == labelId);
            if (label == null)
            {
                await LogAndPublishAsync($"DeleteLabel Unknown Label [{labelId}]", LogLevel.Warning).ConfigureAwait(false);
                return new OperationResult<bool>(true, $"Label Not Found [{labelId}]");
            }

            try
            {
                await LabelService.DeleteAsync(user, labelId).ConfigureAwait(false);
                CacheManager.ClearRegion(label.CacheRegion);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await LogAndPublishAsync("Error deleting Label.").ConfigureAwait(false);
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublishAsync($"DeleteLabel `{label}`, By User `{user}`", LogLevel.Information).ConfigureAwait(false);
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteReleaseAsync(User user, Guid releaseId, bool? doDeleteFiles)
        {
            var sw = new Stopwatch();
            sw.Start();

            var errors = new List<Exception>();

            var release = DbContext.Releases.Include(x => x.Artist).FirstOrDefault(x => x.RoadieId == releaseId);
            try
            {
                if (release == null)
                {
                    await LogAndPublishAsync($"DeleteRelease Unknown Release [{releaseId}]", LogLevel.Warning).ConfigureAwait(false);
                    return new OperationResult<bool>(true, $"Release Not Found [{releaseId}]");
                }
                await ReleaseService.DeleteAsync(user, release, doDeleteFiles ?? false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await LogAndPublishAsync("Error deleting release.").ConfigureAwait(false);
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublishAsync($"DeleteRelease `{release}`, By User `{user}`", LogLevel.Information).ConfigureAwait(false);
            CacheManager.Clear();
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteReleaseSecondaryImageAsync(User user, Guid releaseId, int index)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var release = DbContext.Releases.Include(x => x.Artist).FirstOrDefault(x => x.RoadieId == releaseId);
            if (release == null)
            {
                await LogAndPublishAsync($"DeleteReleaseSecondaryImage Unknown Release [{releaseId}]", LogLevel.Warning).ConfigureAwait(false);
                return new OperationResult<bool>(true, $"Release Not Found [{releaseId}]");
            }

            try
            {
                var releaseFolder = release.ReleaseFileFolder(release.Artist.ArtistFileFolder(Configuration));
                var releaseImagesInFolder = ImageHelper.FindImageTypeInDirectory(new DirectoryInfo(releaseFolder), ImageType.ReleaseSecondary, SearchOption.TopDirectoryOnly);
                var releaseImageFilename = releaseImagesInFolder.Skip(index).FirstOrDefault();
                if (releaseImageFilename.Exists)
                {
                    releaseImageFilename.Delete();
                }
                CacheManager.ClearRegion(release.CacheRegion);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await LogAndPublishAsync("Error deleting release secondary image.").ConfigureAwait(false);
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublishAsync($"DeleteReleaseSecondaryImage `{release}` Index [{index}], By User `{user}`", LogLevel.Information).ConfigureAwait(false);
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteTracksAsync(User user, IEnumerable<Guid> trackIds, bool? doDeleteFile)
        {
            var sw = new Stopwatch();
            sw.Start();

            var errors = new List<Exception>();

            foreach (var trackId in trackIds)
            {
                var track = DbContext.Tracks.Include(x => x.ReleaseMedia)
                    .Include(x => x.ReleaseMedia.Release)
                    .Include(x => x.ReleaseMedia.Release.Artist)
                    .FirstOrDefault(x => x.RoadieId == trackId);
                try
                {
                    if (track == null)
                    {
                        await LogAndPublishAsync($"DeleteTracks Unknown Track [{trackId}]", LogLevel.Warning).ConfigureAwait(false);
                        return new OperationResult<bool>(true, $"Track Not Found [{trackId}]");
                    }

                    DbContext.Tracks.Remove(track);
                    await DbContext.SaveChangesAsync().ConfigureAwait(false);
                    if (doDeleteFile ?? false)
                    {
                        string trackPath = null;
                        try
                        {
                            trackPath = track.PathToTrack(Configuration);
                            if (File.Exists(trackPath))
                            {
                                File.Delete(trackPath);
                                Logger.LogWarning($"x For Track `{track}`, Deleted File [{trackPath}]");
                            }

                            var trackThumbnailName = track.PathToTrackThumbnail(Configuration);
                            if (File.Exists(trackThumbnailName))
                            {
                                File.Delete(trackThumbnailName);
                                Logger.LogWarning($"x For Track `{track}`, Deleted Thumbnail File [{trackThumbnailName}]");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, $"Error Deleting File [{trackPath}] For Track [{track.Id}] Exception [{ex.Serialize()}]");
                        }
                    }
                    await ReleaseService.ScanReleaseFolderAsync(user, track.ReleaseMedia.Release.RoadieId, false, track.ReleaseMedia.Release).ConfigureAwait(false);
                    await BookmarkService.RemoveAllBookmarksForItemAsync(BookmarkType.Track, track.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                    await LogAndPublishAsync("Error deleting track.").ConfigureAwait(false);
                    errors.Add(ex);
                }

                sw.Stop();
                await LogAndPublishAsync($"DeleteTracks `{track}`, By User `{user}`", LogLevel.Warning).ConfigureAwait(false);
            }
            CacheManager.Clear();
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteUserAsync(User applicationUser, Guid userId)
        {
            var sw = new Stopwatch();
            sw.Start();

            var errors = new List<Exception>();
            var user = DbContext.Users.FirstOrDefault(x => x.RoadieId == userId);
            if (user.Id == applicationUser.Id)
            {
                var ex = new Exception("User cannot self.");
                Logger.LogError(ex);
                await LogAndPublishAsync("Error deleting user.").ConfigureAwait(false);
                errors.Add(ex);
            }

            try
            {
                if (user == null)
                {
                    await LogAndPublishAsync($"DeleteUser Unknown User [{userId}]", LogLevel.Warning).ConfigureAwait(false);
                    return new OperationResult<bool>(true, $"User Not Found [{userId}]");
                }

                DbContext.Users.Remove(user);
                await DbContext.SaveChangesAsync().ConfigureAwait(false);
                var userImageFilename = user.PathToImage(Configuration);
                if (File.Exists(userImageFilename))
                {
                    File.Delete(userImageFilename);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await LogAndPublishAsync("Error deleting user.").ConfigureAwait(false);
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublishAsync($"DeleteUser `{user}`, By User `{user}`", LogLevel.Warning).ConfigureAwait(false);
            CacheManager.Clear();
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        /// <summary>
        ///     This is a very simple way to seed the database or setup configuration when the first (who becomes "Admin") user registers
        /// </summary>
        public async Task<OperationResult<bool>> DoInitialSetupAsync(User user, UserManager<User> userManager)
        {
            var sw = new Stopwatch();
            sw.Start();

            // Create user roles
            DbContext.UserRoles.Add(new UserRole
            {
                Id = 1,
                Status = (short)Statuses.Ok,
                CreatedDate = DateTime.UtcNow,
                IsLocked = false,
                RoadieId = Guid.NewGuid().ToString(),
                Name = "Admin",
                Description = "Users with Administrative (full) access",
                NormalizedName = "ADMIN"
            });

            DbContext.UserRoles.Add(new UserRole
            {
                Id = 2,
                Status = (short)Statuses.Ok,
                CreatedDate = DateTime.UtcNow,
                IsLocked = false,
                RoadieId = Guid.NewGuid().ToString(),
                Name = "Editor",
                Description = "Users who have Edit Permissions",
                NormalizedName = "EDITOR"
            });
            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            // Add given user to Admin role
            await userManager.AddToRoleAsync(user, "Admin").ConfigureAwait(false);

            // Create special system artists of 'Sound Tracks' and 'Various Artists'
            DbContext.Artists.Add(new data.Artist
            {
                AlternateNames =
                    "Sound Track|Film Sound Track|Film Sound Tracks|Les Sound Track|Motion Picture Soundtrack|Original Motion Picture SoundTrack|Original Motion Picture SoundTracks|Original Cast Album|Original Soundtrack|Soundtracks|SoundTrack|soundtracks|Original Cast|Original Cast Soundtrack|Motion Picture Cast Recording|Cast Recording",
                ArtistType = "Meta",
                BioContext =
                    "A soundtrack, also written sound track, can be recorded music accompanying and synchronized to the images of a motion picture, book, television program or video game; a commercially released soundtrack album of music as featured in the soundtrack of a film or TV show; or the physical area of a film that contains the synchronized recorded sound.",
                Name = "Sound Tracks",
                SortName = "Sound Tracks",
                Status = Statuses.Ok,
                Tags =
                    "movie and television soundtracks|video game soundtracks|book soundstracks|composite|compilations",
                URLs = "https://en.wikipedia.org/wiki/Soundtrack"
            });
            DbContext.Artists.Add(new data.Artist
            {
                AlternateNames = "Various Artists|Various BNB artist|variousartist|va",
                ArtistType = "Meta",
                BioContext =
                    "Songs included on a compilation album may be previously released or unreleased, usually from several separate recordings by either one or several performers. If by one artist, then generally the tracks were not originally intended for release together as a single work, but may be collected together as a greatest hits album or box set. If from several performers, there may be a theme, topic, or genre which links the tracks, or they may have been intended for release as a single work—such as a tribute album. When the tracks are by the same recording artist, the album may be referred to as a retrospective album or an anthology. Compilation albums may employ traditional product bundling strategies",
                Name = "Various Artists",
                SortName = "Various Artist",
                Status = Statuses.Ok,
                Tags = "compilations|various",
                URLs = "https://en.wikipedia.org/wiki/Compilation_album"
            });
            await DbContext.SaveChangesAsync().ConfigureAwait(false);

            return new OperationResult<bool>
            {
                Data = true,
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public Task<OperationResult<Dictionary<string, List<string>>>> MissingCollectionReleasesAsync(User user)
        {
            var sw = Stopwatch.StartNew();
            sw.Start();
            var missingData = new Dictionary<string, List<string>>();

            foreach (var collection in DbContext.Collections.Where(x => x.Status != Statuses.Complete))
            {
                var collectionReleases = from cr in DbContext.CollectionReleases
                                         where cr.CollectionId == collection.Id
                                         select cr;
                data.PositionArtistRelease[] pars = null;

                try
                {
                    pars = collection.PositionArtistReleases().ToArray();
                }
                catch (Exception ex)
                {
                    missingData.Add($"CSV Error [{collection.Name}]", new List<string> { ex.Message });
                    continue;
                }

                foreach (var par in pars)
                {
                    var cr = collectionReleases.FirstOrDefault(x => x.ListNumber == par.Position);
                    if (cr == null)
                    {
                        // If artist is already in result then add missing album to artist, if not then add artist then add missing album
                        if (!missingData.ContainsKey(par.Artist))
                        {
                            missingData.Add(par.Artist, new List<string>());
                        }
                        var r = $"[{collection.Name}]:[{par.Release}]";
                        missingData[par.Artist].Add(r);
                    }
                }
            }

            sw.Stop();
            return Task.FromResult(new OperationResult<Dictionary<string, List<string>>>
            {
                Data = missingData.OrderBy(x => x.Value.Count).ToDictionary(x => x.Key, x => x.Value),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }


        public void PerformStartUpTasks()
        {
            var sw = Stopwatch.StartNew();

            #region Setup Configured storage folders

            try
            {
                if (!Directory.Exists(Configuration.LibraryFolder))
                {
                    Directory.CreateDirectory(Configuration.LibraryFolder);
                    Logger.LogInformation($"Created Library Folder [{Configuration.LibraryFolder }]");
                }
                if (!Directory.Exists(Configuration.InboundFolder))
                {
                    Directory.CreateDirectory(Configuration.InboundFolder);
                    Logger.LogInformation($"Created Inbound Folder [{Configuration.InboundFolder }]");
                }
                if (!Directory.Exists(Configuration.UserImageFolder))
                {
                    Directory.CreateDirectory(Configuration.UserImageFolder);
                    Logger.LogInformation($"Created User Image Folder [{Configuration.UserImageFolder }]");
                }
                if (!Directory.Exists(Configuration.GenreImageFolder))
                {
                    Directory.CreateDirectory(Configuration.GenreImageFolder);
                    Logger.LogInformation($"Created Genre Image Folder [{Configuration.GenreImageFolder }]");
                }
                if (!Directory.Exists(Configuration.PlaylistImageFolder))
                {
                    Directory.CreateDirectory(Configuration.PlaylistImageFolder);
                    Logger.LogInformation($"Created Playlist Image Folder [{Configuration.PlaylistImageFolder }]");
                }
                if (!Directory.Exists(Configuration.CollectionImageFolder))
                {
                    Directory.CreateDirectory(Configuration.CollectionImageFolder);
                    Logger.LogInformation($"Created Collection Image Folder [{Configuration.CollectionImageFolder }]");
                }
                if (!Directory.Exists(Configuration.LabelImageFolder))
                {
                    Directory.CreateDirectory(Configuration.LabelImageFolder);
                    Logger.LogInformation($"Created Label Image Folder [{Configuration.LabelImageFolder}]");
                }
                if (Configuration.DbContextToUse != DbContexts.MySQL)
                {
                    if (!Directory.Exists(Configuration.FileDatabaseOptions.DatabaseFolder))
                    {
                        Directory.CreateDirectory(Configuration.FileDatabaseOptions.DatabaseFolder);
                        Logger.LogInformation($"Created File Database Folder [{Configuration.FileDatabaseOptions.DatabaseFolder}]");
                    }
                    else
                    {
                        Logger.LogInformation($"Administration Information: DatabaseFolder [{ Configuration.FileDatabaseOptions.DatabaseFolder }], Size [{ GetDirectorySize(Configuration.FileDatabaseOptions.DatabaseFolder) }] ");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error setting up storage folders. Ensure application has create folder permissions.");
                throw;
            }

            #endregion Setup Configured storage folders

            sw.Stop();
            Logger.LogInformation($"Administration Information: LibraryFolder [{ Configuration.LibraryFolder }]");
            Logger.LogInformation($"Administration Information: InboundFolder [{ Configuration.InboundFolder }]");
            Logger.LogInformation($"Administration startup tasks completed, elapsed time [{ sw.ElapsedMilliseconds }]");
        }

        private static long GetDirectorySize(string folderPath)
        {
            DirectoryInfo di = new DirectoryInfo(folderPath);
            return di.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length);
        }

        public async Task<OperationResult<bool>> ScanAllCollectionsAsync(User user, bool isReadOnly = false, bool doPurgeFirst = false)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();

            var collections = await DbContext.Collections.Where(x => x.IsLocked == false).ToArrayAsync().ConfigureAwait(false);
            var updatedReleaseIds = new List<int>();
            foreach (var collection in collections)
            {
                try
                {
                    var result = await ScanCollectionAsync(user, collection.RoadieId, isReadOnly, doPurgeFirst, false).ConfigureAwait(false);
                    if (!result.IsSuccess)
                    {
                        errors.AddRange(result.Errors);
                    }
                    updatedReleaseIds.AddRange((int[])result.AdditionalData["updatedReleaseIds"]);
                }
                catch (Exception ex)
                {
                    await LogAndPublishAsync(ex.ToString(), LogLevel.Error).ConfigureAwait(false);
                    errors.Add(ex);
                }
            }

            foreach (var updatedReleaseId in updatedReleaseIds.Distinct())
            {
                await UpdateReleaseRank(updatedReleaseId).ConfigureAwait(false);
            }
            sw.Stop();
            await LogAndPublishAsync($"ScanAllCollections, By User `{user}`, Updated Release Count [{updatedReleaseIds.Distinct().Count()}], ElapsedTime [{sw.ElapsedMilliseconds}]", LogLevel.Warning).ConfigureAwait(false);
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> ScanArtistAsync(User user, Guid artistId, bool isReadOnly = false)
        {
            var sw = Stopwatch.StartNew();

            var errors = new List<Exception>();
            var artist = DbContext.Artists.FirstOrDefault(x => x.RoadieId == artistId);
            if (artist == null)
            {
                await LogAndPublishAsync($"ScanArtist Unknown Artist [{artistId}]", LogLevel.Warning).ConfigureAwait(false);
                return new OperationResult<bool>(true, $"Artist Not Found [{artistId}]");
            }

            try
            {
                await ArtistService.ScanArtistReleasesFoldersAsync(user, artist.RoadieId, Configuration.LibraryFolder, isReadOnly).ConfigureAwait(false);
                CacheManager.ClearRegion(artist.CacheRegion);
            }
            catch (Exception ex)
            {
                await LogAndPublishAsync(ex.ToString(), LogLevel.Error).ConfigureAwait(false);
                errors.Add(ex);
            }

            sw.Stop();
            DbContext.ScanHistories.Add(new data.ScanHistory
            {
                UserId = user.Id,
                ForArtistId = artist.Id,
                NewReleases = ReleaseLookupEngine.AddedReleaseIds.Count(),
                NewTracks = ReleaseService.AddedTrackIds.Count(),
                TimeSpanInSeconds = (int)sw.Elapsed.TotalSeconds
            });
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            await UpdateArtistRank(artist.Id, true).ConfigureAwait(false);
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                AdditionalData = new Dictionary<string, object> { { "artistAverage", artist.Rating } },
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> ScanArtistsAsync(User user, IEnumerable<Guid> artistIds, bool isReadOnly = false)
        {
            var sw = Stopwatch.StartNew();

            var errors = new List<Exception>();
            foreach (var artistId in artistIds)
            {
                var result = await ScanArtistAsync(user, artistId, isReadOnly).ConfigureAwait(false);
                if (!result.IsSuccess && (result.Errors?.Any() ?? false))
                {
                    errors.AddRange(result.Errors);
                }
            }
            sw.Stop();
            await LogAndPublishAsync($"** Completed! ScanArtists: Artist Count [{ artistIds.Count() }], Elapsed Time [{sw.Elapsed}]").ConfigureAwait(false);
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> ScanCollectionAsync(User user, Guid collectionId, bool isReadOnly = false, bool doPurgeFirst = false, bool doUpdateRanks = true)
        {
            var sw = new Stopwatch();
            sw.Start();

            var releaseIdsInCollection = new List<int>();
            var updatedReleaseIds = new List<int>();
            var errors = new List<Exception>();
            var collection = await DbContext.Collections.FirstOrDefaultAsync(x => x.RoadieId == collectionId).ConfigureAwait(false);
            if (collection == null)
            {
                await LogAndPublishAsync($"ScanCollection Unknown Collection [{collectionId}]", LogLevel.Warning).ConfigureAwait(false);
                return new OperationResult<bool>(true, $"Collection Not Found [{collectionId}]");
            }

            try
            {
                if (doPurgeFirst)
                {
                    await LogAndPublishAsync($"ScanCollection Purging Collection [{collectionId}]", LogLevel.Warning).ConfigureAwait(false);
                    var crs = await DbContext.CollectionReleases.Where(x => x.CollectionId == collection.Id).ToArrayAsync().ConfigureAwait(false);
                    DbContext.CollectionReleases.RemoveRange(crs);
                    await DbContext.SaveChangesAsync().ConfigureAwait(false);
                }

                var collectionMissingRecords = DbContext.CollectionMissings.Where(x => x.CollectionId == collection.Id);
                DbContext.CollectionMissings.RemoveRange(collectionMissingRecords);
                await DbContext.SaveChangesAsync().ConfigureAwait(false);

                var par = collection.PositionArtistReleases();
                if (par != null)
                {
                    var now = DateTime.UtcNow;
                    foreach (var csvRelease in par)
                    {
                        IEnumerable<data.Artist> artistsMatchingName = Enumerable.Empty<data.Artist>();
                        data.Release release = null;

                        var isArtistNameDbKey = csvRelease.Artist.StartsWith(Roadie.Library.Data.Collection.DatabaseIdKey);
                        int? artistId = isArtistNameDbKey ? SafeParser.ToNumber<int?>(csvRelease.Artist.Replace(Roadie.Library.Data.Collection.DatabaseIdKey, string.Empty)) : null;
                        if (artistId.HasValue)
                        {
                            var artist = DbContext.Artists.FirstOrDefault(x => x.Id == artistId.Value);
                            if (artist != null)
                            {
                                artistsMatchingName = new data.Artist[] { artist };
                            }
                        }
                        else
                        {
                            artistsMatchingName = await ArtistLookupEngine.DatabaseQueryForArtistName(csvRelease.Artist).ConfigureAwait(false);
                            if (artistsMatchingName?.Any() != true)
                            {
                                await LogAndPublishAsync($"CSV Position [{ csvRelease.Position }] Unable To Find Artist [{csvRelease.Artist}]", LogLevel.Warning).ConfigureAwait(false);
                                csvRelease.Status = Statuses.Missing;
                                await DbContext.CollectionMissings.AddAsync(new data.CollectionMissing
                                {
                                    CollectionId = collection.Id,
                                    Position = csvRelease.Position,
                                    Artist = csvRelease.Artist,
                                    Release = csvRelease.Release
                                }).ConfigureAwait(false);
                                continue;
                            }
                            else if (artistsMatchingName.Count() > 1)
                            {
                                await LogAndPublishAsync($"CSV Position [{ csvRelease.Position }] Found [{ artistsMatchingName.Count() }] Artists by [{csvRelease.Artist}]", LogLevel.Information).ConfigureAwait(false);
                            }
                        }
                        foreach (var artist in artistsMatchingName)
                        {
                            var isReleaseNameDbKey = csvRelease.Release.StartsWith(Roadie.Library.Data.Collection.DatabaseIdKey);
                            int? releaseId = isReleaseNameDbKey ? SafeParser.ToNumber<int?>(csvRelease.Release.Replace(Roadie.Library.Data.Collection.DatabaseIdKey, string.Empty)) : null;
                            if (releaseId.HasValue)
                            {
                                release = await DbContext.Releases.FirstOrDefaultAsync(x => x.Id == releaseId.Value).ConfigureAwait(false);
                            }
                            else
                            {
                                release = await ReleaseLookupEngine.DatabaseQueryForReleaseTitle(artist, csvRelease.Release).ConfigureAwait(false);
                            }
                            if (release != null)
                            {
                                break;
                            }
                        }

                        if (release == null)
                        {
                            await LogAndPublishAsync($"CSV Position [{ csvRelease.Position }] Unable To Find Release [{csvRelease.Release}], for Artist [{csvRelease.Artist}]", LogLevel.Warning).ConfigureAwait(false);
                            csvRelease.Status = Statuses.Missing;
                            await DbContext.CollectionMissings.AddAsync(new data.CollectionMissing
                            {
                                CollectionId = collection.Id,
                                IsArtistFound = true,
                                Position = csvRelease.Position,
                                Artist = csvRelease.Artist,
                                Release = csvRelease.Release
                            }).ConfigureAwait(false);
                            continue;
                        }

                        var isInCollection = await DbContext.CollectionReleases.FirstOrDefaultAsync(x =>
                            x.CollectionId == collection.Id &&
                            x.ListNumber == csvRelease.Position &&
                            x.ReleaseId == release.Id)
                            .ConfigureAwait(false);
                        var updated = false;
                        // Found in Database but not in collection add to Collection
                        if (isInCollection == null)
                        {
                            await DbContext.CollectionReleases.AddAsync(new data.CollectionRelease
                            {
                                CollectionId = collection.Id,
                                ReleaseId = release.Id,
                                ListNumber = csvRelease.Position
                            }).ConfigureAwait(false);
                            updated = true;
                        }
                        // If Item in Collection is at different List number update CollectionRelease
                        else if (isInCollection.ListNumber != csvRelease.Position)
                        {
                            isInCollection.LastUpdated = now;
                            isInCollection.ListNumber = csvRelease.Position;
                            updated = true;
                        }

                        if (updated && !updatedReleaseIds.Any(x => x == release.Id))
                        {
                            updatedReleaseIds.Add(release.Id);
                        }
                        releaseIdsInCollection.Add(release.Id);
                    }

                    collection.LastUpdated = now;
                    await DbContext.SaveChangesAsync().ConfigureAwait(false);
                    var dto = new CollectionList
                    {
                        CollectionCount = collection.CollectionCount,
                        CollectionFoundCount = (from cr in DbContext.CollectionReleases
                                                where cr.CollectionId == collection.Id
                                                select cr.CollectionId).Count()
                    };
                    if (dto.PercentComplete == 100)
                    {
                        // Lock so future implicit scans dont happen, with DB RI when releases are deleted they are removed from collection
                        collection.IsLocked = true;
                        collection.Status = Statuses.Complete;
                    }
                    else
                    {
                        collection.Status = Statuses.Incomplete;
                    }

                    var collectionReleasesToRemove = await (from cr in DbContext.CollectionReleases
                                                            where cr.CollectionId == collection.Id
                                                            where !releaseIdsInCollection.Contains(cr.ReleaseId)
                                                            select cr).ToArrayAsync().ConfigureAwait(false);
                    if (collectionReleasesToRemove.Length > 0)
                    {
                        await LogAndPublishAsync($"Removing [{collectionReleasesToRemove.Length}] Stale Release Records from Collection.", LogLevel.Information).ConfigureAwait(false);
                        DbContext.CollectionReleases.RemoveRange(collectionReleasesToRemove);
                    }

                    await DbContext.SaveChangesAsync().ConfigureAwait(false);
                    if (doUpdateRanks)
                    {
                        foreach (var updatedReleaseId in updatedReleaseIds)
                        {
                            await UpdateReleaseRank(updatedReleaseId).ConfigureAwait(false);
                        }
                    }
                    CacheManager.ClearRegion(collection.CacheRegion);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                errors.Add(ex);
            }

            sw.Stop();
            Logger.LogWarning($"RescanCollection `{collection}`, By User `{user}`, ElapsedTime [{sw.ElapsedMilliseconds}]");

            return new OperationResult<bool>
            {
                AdditionalData = new Dictionary<string, object> { { "updatedReleaseIds", updatedReleaseIds.ToArray() } },
                IsSuccess = errors.Count == 0,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public Task<OperationResult<bool>> ScanInboundFolderAsync(User user, bool isReadOnly = false) => ScanFolderAsync(user, new DirectoryInfo(Configuration.InboundFolder), isReadOnly);

        public Task<OperationResult<bool>> ScanLibraryFolderAsync(User user, bool isReadOnly = false) => ScanFolderAsync(user, new DirectoryInfo(Configuration.LibraryFolder), isReadOnly, false);

        public async Task<OperationResult<bool>> ScanReleaseAsync(User user, Guid releaseId, bool isReadOnly = false, bool wasDoneForInvalidTrackPlay = false)
        {
            var sw = new Stopwatch();
            sw.Start();

            var errors = new List<Exception>();
            var release = DbContext.Releases
                .Include(x => x.Artist)
                .Include(x => x.Labels)
                .FirstOrDefault(x => x.RoadieId == releaseId);
            if (release == null)
            {
                await LogAndPublishAsync($"ScanRelease Unknown Release [{releaseId}]", LogLevel.Warning).ConfigureAwait(false);
                return new OperationResult<bool>(true, $"Release Not Found [{releaseId}]");
            }

            try
            {
                await ReleaseService.ScanReleaseFolderAsync(user, release.RoadieId, isReadOnly, release).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await LogAndPublishAsync(ex.ToString(), LogLevel.Error).ConfigureAwait(false);
                errors.Add(ex);
            }

            sw.Stop();

            DbContext.ScanHistories.Add(new data.ScanHistory
            {
                UserId = user.Id,
                ForReleaseId = release.Id,
                NewTracks = ReleaseService.AddedTrackIds.Count(),
                TimeSpanInSeconds = (int)sw.Elapsed.TotalSeconds
            });
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> ScanLastGiveNumberOfReleasesAsync(User user, int count, bool isReadOnly = false, bool wasDoneForInvalidTrackPlay = false)
        {
            var sw = Stopwatch.StartNew();

            var errors = new List<Exception>();
            var releaseIds = await DbContext.Releases.OrderByDescending(x => x.CreatedDate).Select(x => x.RoadieId).Take(count).ToListAsync().ConfigureAwait(false);
            foreach (var releaseId in releaseIds)
            {
                var result = await ScanReleaseAsync(user, releaseId, isReadOnly, wasDoneForInvalidTrackPlay).ConfigureAwait(false);
                if (!result.IsSuccess && (result.Errors?.Any() ?? false))
                {
                    errors.AddRange(result.Errors);
                }
            }
            sw.Stop();
            await LogAndPublishAsync($"** Completed! ScanLastGiveNumberOfReleasesAsync: Release Count [{ releaseIds.Count() }], Elapsed Time [{sw.Elapsed}]").ConfigureAwait(false);
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> ScanReleasesAsync(User user, IEnumerable<Guid> releaseIds, bool isReadOnly = false, bool wasDoneForInvalidTrackPlay = false)
        {
            var sw = Stopwatch.StartNew();

            var errors = new List<Exception>();
            foreach (var releaseId in releaseIds)
            {
                var result = await ScanReleaseAsync(user, releaseId, isReadOnly, wasDoneForInvalidTrackPlay).ConfigureAwait(false);
                if (!result.IsSuccess && (result.Errors?.Any() ?? false))
                {
                    errors.AddRange(result.Errors);
                }
            }
            sw.Stop();
            await LogAndPublishAsync($"** Completed! ScanReleases: Release Count [{ releaseIds.Count() }], Elapsed Time [{sw.Elapsed}]").ConfigureAwait(false);
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> UpdateInviteTokenUsedAsync(Guid? tokenId)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            if (tokenId == null)
            {
                return new OperationResult<bool>(true, $"Invalid Invite TokenId [{tokenId}]");
            }
            var token = DbContext.InviteTokens.FirstOrDefault(x => x.RoadieId == tokenId);
            if (token == null)
            {
                return new OperationResult<bool>(true, $"Invite Token Not Found [{tokenId}]");
            }
            token.Status = Statuses.Complete;
            token.LastUpdated = DateTime.UtcNow;
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> ValidateInviteTokenAsync(Guid? tokenId)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            if (tokenId == null)
            {
                return new OperationResult<bool>(true, $"Invalid Invite TokenId [{tokenId}]");
            }
            var token = DbContext.InviteTokens.FirstOrDefault(x => x.RoadieId == tokenId);
            if (token == null)
            {
                return new OperationResult<bool>(true, $"Invite Token Not Found [{tokenId}]");
            }
            if (token.ExpiresDate < DateTime.UtcNow || token.Status == Statuses.Expired)
            {
                token.Status = Statuses.Expired;
                token.LastUpdated = DateTime.UtcNow;
                await DbContext.SaveChangesAsync().ConfigureAwait(false);
                return new OperationResult<bool>(true, $"Invite Token [{tokenId}] Expired [{ token.ExpiresDate }]");
            }
            if (token.Status == Statuses.Complete)
            {
                return new OperationResult<bool>(true, $"Invite Token [{tokenId}] Already Used");
            }
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }
    }
}