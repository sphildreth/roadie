﻿using Mapster;
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
        protected IHubContext<ScanActivityHub> ScanActivityHub { get; }

        private IArtistService ArtistService { get; }

        private IEventMessageLogger EventMessageLogger { get; }

        private IFileDirectoryProcessorService FileDirectoryProcessorService { get; }

        private IGenreService GenreService { get; }
        private ILabelService LabelService { get; }
        private IArtistLookupEngine ArtistLookupEngine { get; }
        private IReleaseLookupEngine ReleaseLookupEngine { get; }
        private IBookmarkService BookmarkService { get; }

        private IReleaseService ReleaseService { get; }

        public AdminService(IRoadieSettings configuration, IHttpEncoder httpEncoder, IHttpContext httpContext,
                            IRoadieDbContext context, ICacheManager cacheManager, ILogger<ArtistService> logger,
                            IHubContext<ScanActivityHub> scanActivityHub, IFileDirectoryProcessorService fileDirectoryProcessorService, IArtistService artistService,
                            IReleaseService releaseService, IArtistLookupEngine artistLookupEngine, IReleaseLookupEngine releaseLookupEngine, 
                            ILabelService labelService, IGenreService genreService, IBookmarkService bookmarkService
        )
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

        public async Task<OperationResult<bool>> DeleteArtist(User user, Guid artistId, bool deleteFolder)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var artist = DbContext.Artists.FirstOrDefault(x => x.RoadieId == artistId);
            if (artist == null)
            {
                await LogAndPublish($"DeleteArtist Unknown Artist [{artistId}]", LogLevel.Warning);
                return new OperationResult<bool>(true, $"Artist Not Found [{artistId}]");
            }

            try
            {
                var result = await ArtistService.Delete(user, artist, deleteFolder);
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
                await LogAndPublish("Error deleting artist.");
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublish($"DeleteArtist `{artist}`, By User `{user}`", LogLevel.Information);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteArtistReleases(User user, Guid artistId, bool doDeleteFiles = false)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var artist = DbContext.Artists.FirstOrDefault(x => x.RoadieId == artistId);
            if (artist == null)
            {
                await LogAndPublish($"DeleteArtistReleases Unknown Artist [{artistId}]", LogLevel.Warning);
                return new OperationResult<bool>(true, $"Artist Not Found [{artistId}]");
            }

            try
            {
                await ReleaseService.DeleteReleases(user, DbContext.Releases.Where(x => x.ArtistId == artist.Id).Select(x => x.RoadieId).ToArray(), doDeleteFiles);
                await DbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await LogAndPublish("Error deleting artist.");
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublish($"DeleteArtistReleases `{artist}`, By User `{user}`", LogLevel.Information);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteArtistSecondaryImage(User user, Guid artistId, int index)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var artist = DbContext.Artists.FirstOrDefault(x => x.RoadieId == artistId);
            if (artist == null)
            {
                await LogAndPublish($"DeleteArtistSecondaryImage Unknown Artist [{artistId}]", LogLevel.Warning);
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
                await LogAndPublish("Error deleting artist secondary image.");
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublish($"DeleteArtistSecondaryImage `{artist}` Index [{index}], By User `{user}`", LogLevel.Information);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteGenre(User user, Guid genreId)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var genre = DbContext.Genres.FirstOrDefault(x => x.RoadieId == genreId);
            if (genre == null)
            {
                await LogAndPublish($"DeleteLabel Unknown Genre [{genreId}]", LogLevel.Warning);
                return new OperationResult<bool>(true, $"Genre Not Found [{genreId}]");
            }

            try
            {
                await GenreService.Delete(user, genreId);
                CacheManager.ClearRegion(genre.CacheRegion);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await LogAndPublish("Error deleting Genre.");
                errors.Add(ex);
            }

            sw.Stop();            
            await LogAndPublish($"DeleteGenre `{genre}`, By User `{user}`", LogLevel.Information);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteLabel(User user, Guid labelId)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var label = DbContext.Labels.FirstOrDefault(x => x.RoadieId == labelId);
            if (label == null)
            {
                await LogAndPublish($"DeleteLabel Unknown Label [{labelId}]", LogLevel.Warning);
                return new OperationResult<bool>(true, $"Label Not Found [{labelId}]");
            }

            try
            {
                await LabelService.Delete(user, labelId);
                CacheManager.ClearRegion(label.CacheRegion);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await LogAndPublish("Error deleting Label.");
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublish($"DeleteLabel `{label}`, By User `{user}`", LogLevel.Information);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteRelease(User user, Guid releaseId, bool? doDeleteFiles)
        {
            var sw = new Stopwatch();
            sw.Start();

            var errors = new List<Exception>();

            var release = DbContext.Releases.Include(x => x.Artist).FirstOrDefault(x => x.RoadieId == releaseId);
            try
            {
                if (release == null)
                {
                    await LogAndPublish($"DeleteRelease Unknown Release [{releaseId}]", LogLevel.Warning);
                    return new OperationResult<bool>(true, $"Release Not Found [{releaseId}]");
                }
                await ReleaseService.Delete(user, release, doDeleteFiles ?? false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await LogAndPublish("Error deleting release.");
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublish($"DeleteRelease `{release}`, By User `{user}`", LogLevel.Information);
            CacheManager.Clear();
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteReleaseSecondaryImage(User user, Guid releaseId, int index)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();
            var release = DbContext.Releases.Include(x => x.Artist).FirstOrDefault(x => x.RoadieId == releaseId);
            if (release == null)
            {
                await LogAndPublish($"DeleteReleaseSecondaryImage Unknown Release [{releaseId}]", LogLevel.Warning);
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
                await LogAndPublish("Error deleting release secondary image.");
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublish($"DeleteReleaseSecondaryImage `{release}` Index [{index}], By User `{user}`", LogLevel.Information);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteTracks(User user, IEnumerable<Guid> trackIds, bool? doDeleteFile)
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
                        await LogAndPublish($"DeleteTracks Unknown Track [{trackId}]", LogLevel.Warning);
                        return new OperationResult<bool>(true, $"Track Not Found [{trackId}]");
                    }

                    DbContext.Tracks.Remove(track);
                    await DbContext.SaveChangesAsync();
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
                            Logger.LogError(ex, string.Format("Error Deleting File [{0}] For Track [{1}] Exception [{2}]", trackPath, track.Id, ex.Serialize()));
                        }
                    }
                    await ReleaseService.ScanReleaseFolder(user, track.ReleaseMedia.Release.RoadieId, false, track.ReleaseMedia.Release);
                    await BookmarkService.RemoveAllBookmarksForItem(BookmarkType.Track, track.Id);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                    await LogAndPublish("Error deleting track.");
                    errors.Add(ex);
                }

                sw.Stop();
                await LogAndPublish($"DeleteTracks `{track}`, By User `{user}`", LogLevel.Warning);
            }
            CacheManager.Clear();
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteUser(User applicationUser, Guid userId)
        {
            var sw = new Stopwatch();
            sw.Start();

            var errors = new List<Exception>();
            var user = DbContext.Users.FirstOrDefault(x => x.RoadieId == userId);
            if (user.Id == applicationUser.Id)
            {
                var ex = new Exception("User cannot self.");
                Logger.LogError(ex);
                await LogAndPublish("Error deleting user.");
                errors.Add(ex);
            }

            try
            {
                if (user == null)
                {
                    await LogAndPublish($"DeleteUser Unknown User [{userId}]", LogLevel.Warning);
                    return new OperationResult<bool>(true, $"User Not Found [{userId}]");
                }

                DbContext.Users.Remove(user);
                await DbContext.SaveChangesAsync();
                var userImageFilename = user.PathToImage(Configuration);
                if (File.Exists(userImageFilename))
                {
                    File.Delete(userImageFilename);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await LogAndPublish("Error deleting user.");
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublish($"DeleteUser `{user}`, By User `{user}`", LogLevel.Warning);
            CacheManager.Clear();
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        /// <summary>
        ///     This is a very simple way to seed the database or setup configuration when the first (who becomes "Admin") user registers
        /// </summary>
        public async Task<OperationResult<bool>> DoInitialSetup(User user, UserManager<User> userManager)
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
            await DbContext.SaveChangesAsync();

            // Add given user to Admin role
            await userManager.AddToRoleAsync(user, "Admin");

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
            await DbContext.SaveChangesAsync();

            return new OperationResult<bool>
            {
                Data = true,
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public Task<OperationResult<Dictionary<string, List<string>>>> MissingCollectionReleases(User user)
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
                Data = missingData.OrderBy(x => x.Value.Count()).ToDictionary(x => x.Key, x => x.Value),
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            });
        }

        /// <summary>
        /// Perform checks/setup on start of application
        /// </summary>
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
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error setting up storage folders. Ensure application has create folder permissions.");
                throw;
            }

            #endregion Setup Configured storage folders

            sw.Stop();
            Logger.LogInformation($"Administration startup tasks completed, elapsed time [{ sw.ElapsedMilliseconds }]");
        }

        public async Task<OperationResult<bool>> ScanAllCollections(User user, bool isReadOnly = false, bool doPurgeFirst = false)
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
                    var result = await ScanCollection(user, collection.RoadieId, isReadOnly, doPurgeFirst, false).ConfigureAwait(false);
                    if (!result.IsSuccess)
                    {
                        errors.AddRange(result.Errors);
                    }
                    updatedReleaseIds.AddRange((int[])result.AdditionalData["updatedReleaseIds"]);
                }
                catch (Exception ex)
                {
                    await LogAndPublish(ex.ToString(), LogLevel.Error).ConfigureAwait(false);
                    errors.Add(ex);
                }
            }

            foreach (var updatedReleaseId in updatedReleaseIds.Distinct())
            {
                await UpdateReleaseRank(updatedReleaseId).ConfigureAwait(false);
            }
            sw.Stop();
            await LogAndPublish($"ScanAllCollections, By User `{user}`, Updated Release Count [{updatedReleaseIds.Distinct().Count()}], ElapsedTime [{sw.ElapsedMilliseconds}]", LogLevel.Warning).ConfigureAwait(false);
            return new OperationResult<bool>
            {
                IsSuccess = errors.Count == 0,
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> ScanArtist(User user, Guid artistId, bool isReadOnly = false)
        {
            var sw = Stopwatch.StartNew();

            var errors = new List<Exception>();
            var artist = DbContext.Artists.FirstOrDefault(x => x.RoadieId == artistId);
            if (artist == null)
            {
                await LogAndPublish($"ScanArtist Unknown Artist [{artistId}]", LogLevel.Warning);
                return new OperationResult<bool>(true, $"Artist Not Found [{artistId}]");
            }

            try
            {
                var result = await ArtistService.ScanArtistReleasesFolders(user, artist.RoadieId, Configuration.LibraryFolder, isReadOnly);
                CacheManager.ClearRegion(artist.CacheRegion);
            }
            catch (Exception ex)
            {
                await LogAndPublish(ex.ToString(), LogLevel.Error);
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
            await DbContext.SaveChangesAsync();
            await UpdateArtistRank(artist.Id, true);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                AdditionalData = new Dictionary<string, object> { { "artistAverage", artist.Rating } },
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> ScanArtists(User user, IEnumerable<Guid> artistIds, bool isReadOnly = false)
        {
            var sw = Stopwatch.StartNew();

            var errors = new List<Exception>();
            foreach (var artistId in artistIds)
            {
                var result = await ScanArtist(user, artistId, isReadOnly);
                if (!result.IsSuccess)
                {
                    if (result.Errors?.Any() ?? false)
                    {
                        errors.AddRange(result.Errors);
                    }
                }
            }
            sw.Stop();
            await LogAndPublish($"** Completed! ScanArtists: Artist Count [{ artistIds.Count() }], Elapsed Time [{sw.Elapsed}]");
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> ScanCollection(User user, Guid collectionId, bool isReadOnly = false, bool doPurgeFirst = false, bool doUpdateRanks = true)
        {
            var sw = new Stopwatch();
            sw.Start();

            var releaseIdsInCollection = new List<int>();
            var updatedReleaseIds = new List<int>();
            var result = new List<data.PositionArtistRelease>();
            var errors = new List<Exception>();
            var collection = await DbContext.Collections.FirstOrDefaultAsync(x => x.RoadieId == collectionId).ConfigureAwait(false);
            if (collection == null)
            {
                await LogAndPublish($"ScanCollection Unknown Collection [{collectionId}]", LogLevel.Warning).ConfigureAwait(false);
                return new OperationResult<bool>(true, $"Collection Not Found [{collectionId}]");
            }

            try
            {
                if (doPurgeFirst)
                {
                    await LogAndPublish($"ScanCollection Purging Collection [{collectionId}]", LogLevel.Warning).ConfigureAwait(false);
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
                        int? artistId = isArtistNameDbKey ? SafeParser.ToNumber<int?>(csvRelease.Artist.Replace(Roadie.Library.Data.Collection.DatabaseIdKey, "")) : null;
                        if(artistId.HasValue)
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
                            if (artistsMatchingName == null || !artistsMatchingName.Any())
                            {
                                await LogAndPublish($"CSV Position [{ csvRelease.Position }] Unable To Find Artist [{csvRelease.Artist}]", LogLevel.Warning).ConfigureAwait(false);
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
                                await LogAndPublish($"CSV Position [{ csvRelease.Position }] Found [{ artistsMatchingName.Count() }] Artists by [{csvRelease.Artist}]", LogLevel.Information).ConfigureAwait(false);
                            }
                        }
                        foreach (var artist in artistsMatchingName)
                        {
                            var isReleaseNameDbKey = csvRelease.Release.StartsWith(Roadie.Library.Data.Collection.DatabaseIdKey);
                            int? releaseId = isReleaseNameDbKey ? SafeParser.ToNumber<int?>(csvRelease.Release.Replace(Roadie.Library.Data.Collection.DatabaseIdKey, "")) : null;
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
                            await LogAndPublish($"CSV Position [{ csvRelease.Position }] Unable To Find Release [{csvRelease.Release}], for Artist [{csvRelease.Artist}]", LogLevel.Warning).ConfigureAwait(false);
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
                            });
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
                    if (collectionReleasesToRemove.Any())
                    {
                        await LogAndPublish($"Removing [{collectionReleasesToRemove.Count()}] Stale Release Records from Collection.", LogLevel.Information);
                        DbContext.CollectionReleases.RemoveRange(collectionReleasesToRemove);
                    }

                    await DbContext.SaveChangesAsync();
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
            Logger.LogWarning(string.Format("RescanCollection `{0}`, By User `{1}`, ElapsedTime [{2}]", collection, user, sw.ElapsedMilliseconds));

            return new OperationResult<bool>
            {
                AdditionalData = new Dictionary<string, object> { { "updatedReleaseIds", updatedReleaseIds.ToArray() } },
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> ScanInboundFolder(User user, bool isReadOnly = false)
        {
            var d = new DirectoryInfo(Configuration.InboundFolder);
            return await ScanFolder(user, d, isReadOnly);
        }

        public async Task<OperationResult<bool>> ScanLibraryFolder(User user, bool isReadOnly = false)
        {
            var d = new DirectoryInfo(Configuration.LibraryFolder);
            return await ScanFolder(user, d, isReadOnly, false);
        }

        public async Task<OperationResult<bool>> ScanRelease(User user, Guid releaseId, bool isReadOnly = false, bool wasDoneForInvalidTrackPlay = false)
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
                await LogAndPublish($"ScanRelease Unknown Release [{releaseId}]", LogLevel.Warning);
                return new OperationResult<bool>(true, $"Release Not Found [{releaseId}]");
            }

            try
            {
                var result = await ReleaseService.ScanReleaseFolder(user, release.RoadieId, isReadOnly, release);
            }
            catch (Exception ex)
            {
                await LogAndPublish(ex.ToString(), LogLevel.Error);
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
            await DbContext.SaveChangesAsync();
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> ScanReleases(User user, IEnumerable<Guid> releaseIds, bool isReadOnly = false, bool wasDoneForInvalidTrackPlay = false)
        {
            var sw = Stopwatch.StartNew();

            var errors = new List<Exception>();
            foreach (var releaseId in releaseIds)
            {
                var result = await ScanRelease(user, releaseId, isReadOnly, wasDoneForInvalidTrackPlay);
                if (!result.IsSuccess)
                {
                    if (result.Errors?.Any() ?? false)
                    {
                        errors.AddRange(result.Errors);
                    }
                }
            }
            sw.Stop();
            await LogAndPublish($"** Completed! ScanReleases: Release Count [{ releaseIds.Count() }], Elapsed Time [{sw.Elapsed}]");
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> UpdateInviteTokenUsed(Guid? tokenId)
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
            await DbContext.SaveChangesAsync();
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> ValidateInviteToken(Guid? tokenId)
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
                await DbContext.SaveChangesAsync();
                return new OperationResult<bool>(true, $"Invite Token [{tokenId}] Expired [{ token.ExpiresDate }]");
            }
            if (token.Status == Statuses.Complete)
            {
                return new OperationResult<bool>(true, $"Invite Token [{tokenId}] Already Used");
            }
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        private void EventMessageLogger_Messages(object sender, EventMessage e) => Task.WaitAll(LogAndPublish(e.Message, e.Level));

        private async Task LogAndPublish(string message, LogLevel level = LogLevel.Trace)
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

            await ScanActivityHub.Clients.All.SendAsync("SendSystemActivity", message).ConfigureAwait(false);
        }

        private async Task<OperationResult<bool>> ScanFolder(User user, DirectoryInfo d, bool isReadOnly, bool doDeleteFiles = true)
        {
            var sw = new Stopwatch();
            sw.Start();

            long processedFiles = 0;
            await LogAndPublish($"** Processing Folder: [{d.FullName}]");

            long processedFolders = 0;
            foreach (var folder in Directory.EnumerateDirectories(d.FullName).ToArray())
            {
                var directoryProcessResult = await FileDirectoryProcessorService.Process(user:user, 
                                                                                         folder: new DirectoryInfo(folder), 
                                                                                         doJustInfo: isReadOnly, 
                                                                                         doDeleteFiles: doDeleteFiles);
                processedFolders++;
                processedFiles += SafeParser.ToNumber<int>(directoryProcessResult.AdditionalData["ProcessedFiles"]);
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
            await DbContext.SaveChangesAsync();
            await LogAndPublish($"** Completed! Processed Folders [{processedFolders}], Processed Files [{processedFiles}], New Artists [{ newScanHistory.NewArtists }], New Releases [{ newScanHistory.NewReleases }], New Tracks [{ newScanHistory.NewTracks }] : Elapsed Time [{sw.Elapsed}]");
            return new OperationResult<bool>
            {
                Data = true,
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            };
        }
    }
}