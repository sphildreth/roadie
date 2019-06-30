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
using Roadie.Library.Extensions;
using Roadie.Library.Factories;
using Roadie.Library.Identity;
using Roadie.Library.Imaging;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.FileName;
using Roadie.Library.MetaData.ID3Tags;
using Roadie.Library.MetaData.LastFm;
using Roadie.Library.MetaData.MusicBrainz;
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

        private ILogger MessageLogger => EventMessageLogger as ILogger;

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
            ScanActivityHub = scanActivityHub;
            EventMessageLogger = new EventMessageLogger();
            EventMessageLogger.Messages += EventMessageLogger_Messages;

            MusicBrainzProvider = new MusicBrainzProvider(configuration, cacheManager, MessageLogger);
            LastFmHelper = new LastFmHelper(configuration, cacheManager, MessageLogger, context, httpEncoder);
            FileNameHelper = new FileNameHelper(configuration, cacheManager, MessageLogger);
            ID3TagsHelper = new ID3TagsHelper(configuration, cacheManager, MessageLogger);

            ArtistLookupEngine =
                new ArtistLookupEngine(configuration, httpEncoder, context, cacheManager, MessageLogger);
            LabelLookupEngine = new LabelLookupEngine(configuration, httpEncoder, context, cacheManager, MessageLogger);
            ReleaseLookupEngine = new ReleaseLookupEngine(configuration, httpEncoder, context, cacheManager,
                MessageLogger, ArtistLookupEngine, LabelLookupEngine);
            ImageFactory = new ImageFactory(configuration, httpEncoder, context, cacheManager, MessageLogger,
                ArtistLookupEngine, ReleaseLookupEngine);
            LabelFactory = new LabelFactory(configuration, httpEncoder, context, cacheManager, MessageLogger,
                ArtistLookupEngine, ReleaseLookupEngine);
            AudioMetaDataHelper = new AudioMetaDataHelper(configuration, httpEncoder, context, MusicBrainzProvider,
                LastFmHelper, cacheManager,
                MessageLogger, ArtistLookupEngine, ImageFactory, FileNameHelper, ID3TagsHelper);
            ReleaseFactory = new ReleaseFactory(configuration, httpEncoder, context, cacheManager, MessageLogger,
                ArtistLookupEngine, LabelFactory, AudioMetaDataHelper, ReleaseLookupEngine);
            ArtistFactory = new ArtistFactory(configuration, httpEncoder, context, cacheManager, MessageLogger,
                ArtistLookupEngine, ReleaseFactory, ImageFactory, ReleaseLookupEngine, AudioMetaDataHelper);
        }

        public async Task<OperationResult<bool>> DeleteArtist(ApplicationUser user, Guid artistId)
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
                var result = await ArtistFactory.Delete(artist);
                if (!result.IsSuccess)
                    return new OperationResult<bool>
                    {
                        Errors = result.Errors
                    };
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

        public async Task<OperationResult<bool>> DeleteArtistReleases(ApplicationUser user, Guid artistId,
            bool doDeleteFiles = false)
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
                await ReleaseFactory.DeleteReleases(
                    DbContext.Releases.Where(x => x.ArtistId == artist.Id).Select(x => x.RoadieId).ToArray(),
                    doDeleteFiles);
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

        public async Task<OperationResult<bool>> DeleteArtistSecondaryImage(ApplicationUser user, Guid artistId,
            int index)
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
                var artistFolder = artist.ArtistFileFolder(Configuration, Configuration.LibraryFolder);
                var artistImagesInFolder = ImageHelper.FindImageTypeInDirectory(new DirectoryInfo(artistFolder),
                    ImageType.ArtistSecondary, SearchOption.TopDirectoryOnly);
                var artistImageFilename = artistImagesInFolder.Skip(index).FirstOrDefault();
                if (artistImageFilename.Exists) artistImageFilename.Delete();
                CacheManager.ClearRegion(artist.CacheRegion);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await LogAndPublish("Error deleting artist secondary image.");
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublish($"DeleteArtistSecondaryImage `{artist}` Index [{index}], By User `{user}`",
                LogLevel.Information);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteRelease(ApplicationUser user, Guid releaseId,
            bool? doDeleteFiles)
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

                await ReleaseFactory.Delete(release, doDeleteFiles ?? false);
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

        public async Task<OperationResult<bool>> DeleteReleaseSecondaryImage(ApplicationUser user, Guid releaseId,
            int index)
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
                var releaseFolder =
                    release.ReleaseFileFolder(release.Artist.ArtistFileFolder(Configuration,
                        Configuration.LibraryFolder));
                var releaseImagesInFolder = ImageHelper.FindImageTypeInDirectory(new DirectoryInfo(releaseFolder),
                    ImageType.ReleaseSecondary, SearchOption.TopDirectoryOnly);
                var releaseImageFilename = releaseImagesInFolder.Skip(index).FirstOrDefault();
                if (releaseImageFilename.Exists) releaseImageFilename.Delete();
                CacheManager.ClearRegion(release.CacheRegion);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await LogAndPublish("Error deleting release secondary image.");
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublish($"DeleteReleaseSecondaryImage `{release}` Index [{index}], By User `{user}`",
                LogLevel.Information);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteTrack(ApplicationUser user, Guid trackId, bool? doDeleteFile)
        {
            var sw = new Stopwatch();
            sw.Start();

            var errors = new List<Exception>();

            var track = DbContext.Tracks.Include(x => x.ReleaseMedia)
                .Include(x => x.ReleaseMedia.Release)
                .Include(x => x.ReleaseMedia.Release.Artist)
                .FirstOrDefault(x => x.RoadieId == trackId);
            try
            {
                if (track == null)
                {
                    await LogAndPublish($"DeleteTrack Unknown Track [{trackId}]", LogLevel.Warning);
                    return new OperationResult<bool>(true, $"Track Not Found [{trackId}]");
                }

                DbContext.Tracks.Remove(track);
                await DbContext.SaveChangesAsync();
                if (doDeleteFile ?? false)
                {
                    string trackPath = null;
                    try
                    {
                        trackPath = track.PathToTrack(Configuration, Configuration.LibraryFolder);
                        if (File.Exists(trackPath))
                        {
                            File.Delete(trackPath);
                            Logger.LogWarning($"x For Track `{track}`, Deleted File [{trackPath}]");
                        }

                        var trackThumbnailName = track.PathToTrackThumbnail(Configuration, Configuration.LibraryFolder);
                        if (File.Exists(trackThumbnailName))
                        {
                            File.Delete(trackThumbnailName);
                            Logger.LogWarning($"x For Track `{track}`, Deleted Thumbnail File [{trackThumbnailName}]");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex,
                            string.Format("Error Deleting File [{0}] For Track [{1}] Exception [{2}]", trackPath,
                                track.Id, ex.Serialize()));
                    }
                }

                await ReleaseFactory.ScanReleaseFolder(track.ReleaseMedia.Release.RoadieId, Configuration.LibraryFolder,
                    false, track.ReleaseMedia.Release);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await LogAndPublish("Error deleting track.");
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublish($"DeleteTrack `{track}`, By User `{user}`", LogLevel.Information);
            CacheManager.Clear();
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> DeleteUser(ApplicationUser applicationUser, Guid userId)
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
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                await LogAndPublish("Error deleting user.");
                errors.Add(ex);
            }

            sw.Stop();
            await LogAndPublish($"DeleteUser `{user}`, By User `{user}`", LogLevel.Information);
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
        ///     This is a very simple way to seed the database or setup configuration when the first (who becomes "Admin") user
        ///     registers
        /// </summary>
        public async Task<OperationResult<bool>> DoInitialSetup(ApplicationUser user,
            UserManager<ApplicationUser> userManager)
        {
            var sw = new Stopwatch();
            sw.Start();

            // Create user roles
            DbContext.UserRoles.Add(new ApplicationRole
            {
                Name = "Admin",
                Description = "Users with Administrative (full) access",
                NormalizedName = "ADMIN"
            });
            DbContext.UserRoles.Add(new ApplicationRole
            {
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

        public Task<OperationResult<Dictionary<string, List<string>>>> MissingCollectionReleases(ApplicationUser user)
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
                        if (!missingData.ContainsKey(par.Artist)) missingData.Add(par.Artist, new List<string>());
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

        public async Task<OperationResult<bool>> ScanAllCollections(ApplicationUser user, bool isReadOnly = false,
            bool doPurgeFirst = false)
        {
            var sw = new Stopwatch();
            sw.Start();
            var errors = new List<Exception>();

            var collections = DbContext.Collections.Where(x => x.IsLocked == false).ToArray();
            var updatedReleaseIds = new List<int>();
            foreach (var collection in collections)
                try
                {
                    var result = await ScanCollection(user, collection.RoadieId, isReadOnly, doPurgeFirst, false);
                    if (!result.IsSuccess) errors.AddRange(result.Errors);
                    updatedReleaseIds.AddRange((int[])result.AdditionalData["updatedReleaseIds"]);
                }
                catch (Exception ex)
                {
                    await LogAndPublish(ex.ToString(), LogLevel.Error);
                    errors.Add(ex);
                }

            foreach (var updatedReleaseId in updatedReleaseIds.Distinct()) await UpdateReleaseRank(updatedReleaseId);
            sw.Stop();
            await LogAndPublish(
                $"ScanAllCollections, By User `{user}`, Updated Release Count [{updatedReleaseIds.Distinct().Count()}], ElapsedTime [{sw.ElapsedMilliseconds}]",
                LogLevel.Information);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> ScanArtist(ApplicationUser user, Guid artistId,
            bool isReadOnly = false)
        {
            var sw = new Stopwatch();
            sw.Start();

            var errors = new List<Exception>();
            var artist = DbContext.Artists.FirstOrDefault(x => x.RoadieId == artistId);
            if (artist == null)
            {
                await LogAndPublish($"ScanArtist Unknown Release [{artistId}]", LogLevel.Warning);
                return new OperationResult<bool>(true, $"Artist Not Found [{artistId}]");
            }

            try
            {
                var result =
                    await ArtistFactory.ScanArtistReleasesFolders(artist.RoadieId, Configuration.LibraryFolder,
                        isReadOnly);
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
                NewTracks = ReleaseFactory.AddedTrackIds.Count(),
                TimeSpanInSeconds = (int)sw.Elapsed.TotalSeconds
            });
            await DbContext.SaveChangesAsync();
            await UpdateArtistRank(artist.Id, true);
            await LogAndPublish($"ScanArtist `{artist}`, By User `{user}`", LogLevel.Information);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                AdditionalData = new Dictionary<string, object> { { "artistAverage", artist.Rating } },
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> ScanCollection(ApplicationUser user, Guid collectionId,
            bool isReadOnly = false, bool doPurgeFirst = false, bool doUpdateRanks = true)
        {
            var sw = new Stopwatch();
            sw.Start();

            var releaseIdsInCollection = new List<int>();
            var updatedReleaseIds = new List<int>();
            var result = new List<data.PositionArtistRelease>();
            var errors = new List<Exception>();
            var collection = DbContext.Collections.FirstOrDefault(x => x.RoadieId == collectionId);
            if (collection == null)
            {
                await LogAndPublish($"ScanCollection Unknown Collection [{collectionId}]", LogLevel.Warning);
                return new OperationResult<bool>(true, $"Collection Not Found [{collectionId}]");
            }

            try
            {
                if (doPurgeFirst)
                {
                    await LogAndPublish($"ScanCollection Purgeing Collection [{collectionId}]", LogLevel.Warning);
                    var crs = DbContext.CollectionReleases.Where(x => x.CollectionId == collection.Id).ToArray();
                    DbContext.CollectionReleases.RemoveRange(crs);
                    await DbContext.SaveChangesAsync();
                }

                var collectionMissingRecords = DbContext.CollectionMissings.Where(x => x.CollectionId == collection.Id);
                DbContext.CollectionMissings.RemoveRange(collectionMissingRecords);
                await DbContext.SaveChangesAsync();

                var par = collection.PositionArtistReleases();
                if (par != null)
                {
                    var now = DateTime.UtcNow;
                    foreach (var csvRelease in par)
                    {
                        data.Artist artist = null;
                        data.Release release = null;

                        var searchName = csvRelease.Artist.NormalizeName();
                        var specialSearchName = csvRelease.Artist.ToAlphanumericName();

                        var artistResults = (from a in DbContext.Artists
                                             where a.Name.Contains(searchName) ||
                                                   a.SortName.Contains(searchName) ||
                                                   a.AlternateNames.Contains(searchName) ||
                                                   a.AlternateNames.Contains(specialSearchName)
                                             select a).ToArray();
                        if (!artistResults.Any())
                        {
                            await LogAndPublish(
                                $"Unable To Find Artist [{csvRelease.Artist}], SearchName [{searchName}]",
                                LogLevel.Warning);
                            csvRelease.Status = Statuses.Missing;
                            DbContext.CollectionMissings.Add(new data.CollectionMissing
                            {
                                CollectionId = collection.Id,
                                Position = csvRelease.Position,
                                Artist = csvRelease.Artist,
                                Release = searchName
                            });
                            continue;
                        }

                        foreach (var artistResult in artistResults)
                        {
                            artist = artistResult;
                            searchName = csvRelease.Release.NormalizeName().ToLower();
                            specialSearchName = csvRelease.Release.ToAlphanumericName();
                            release = (from r in DbContext.Releases
                                       where r.ArtistId == artist.Id
                                       where r.Title.Contains(searchName) ||
                                             r.AlternateNames.Contains(searchName) ||
                                             r.AlternateNames.Contains(specialSearchName)
                                       select r
                                ).FirstOrDefault();
                            if (release != null) break;
                        }

                        if (release == null)
                        {
                            await LogAndPublish(
                                $"Unable To Find Release [{csvRelease.Release}] for Artist [{csvRelease.Artist}], SearchName [{searchName}]",
                                LogLevel.Warning);
                            csvRelease.Status = Statuses.Missing;
                            DbContext.CollectionMissings.Add(new data.CollectionMissing
                            {
                                CollectionId = collection.Id,
                                IsArtistFound = true,
                                Position = csvRelease.Position,
                                Artist = csvRelease.Artist,
                                Release = searchName
                            });
                            continue;
                        }

                        var isInCollection = DbContext.CollectionReleases.FirstOrDefault(x =>
                            x.CollectionId == collection.Id &&
                            x.ListNumber == csvRelease.Position &&
                            x.ReleaseId == release.Id);
                        var updated = false;
                        // Found in Database but not in collection add to Collection
                        if (isInCollection == null)
                        {
                            DbContext.CollectionReleases.Add(new data.CollectionRelease
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

                        if (updated && !updatedReleaseIds.Any(x => x == release.Id)) updatedReleaseIds.Add(release.Id);
                        releaseIdsInCollection.Add(release.Id);
                    }

                    collection.LastUpdated = now;
                    await DbContext.SaveChangesAsync();
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

                    var collectionReleasesToRemove = (from cr in DbContext.CollectionReleases
                                                      where cr.CollectionId == collection.Id
                                                      where !releaseIdsInCollection.Contains(cr.ReleaseId)
                                                      select cr).ToArray();
                    if (collectionReleasesToRemove.Any())
                    {
                        await LogAndPublish(
                            $"Removing [{collectionReleasesToRemove.Count()}] Stale Release Records from Collection.",
                            LogLevel.Information);
                        DbContext.CollectionReleases.RemoveRange(collectionReleasesToRemove);
                    }

                    await DbContext.SaveChangesAsync();
                    if (doUpdateRanks)
                        foreach (var updatedReleaseId in updatedReleaseIds)
                            await UpdateReleaseRank(updatedReleaseId);
                    CacheManager.ClearRegion(collection.CacheRegion);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                errors.Add(ex);
            }

            sw.Stop();
            Logger.LogInformation(string.Format("RescanCollection `{0}`, By User `{1}`, ElapsedTime [{2}]", collection,
                user, sw.ElapsedMilliseconds));

            return new OperationResult<bool>
            {
                AdditionalData = new Dictionary<string, object> { { "updatedReleaseIds", updatedReleaseIds.ToArray() } },
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        public async Task<OperationResult<bool>> ScanInboundFolder(ApplicationUser user, bool isReadOnly = false)
        {
            var d = new DirectoryInfo(Configuration.InboundFolder);
            var dest = new DirectoryInfo(Configuration.LibraryFolder);
            return await ScanFolder(d, dest, user, isReadOnly);
        }

        public async Task<OperationResult<bool>> ScanLibraryFolder(ApplicationUser user, bool isReadOnly = false)
        {
            var d = new DirectoryInfo(Configuration.LibraryFolder);
            var dest = new DirectoryInfo(Configuration.LibraryFolder);
            return await ScanFolder(d, dest, user, isReadOnly);
        }

        public async Task<OperationResult<bool>> ScanRelease(ApplicationUser user, Guid releaseId,
            bool isReadOnly = false, bool wasDoneForInvalidTrackPlay = false)
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
                var result = await ReleaseFactory.ScanReleaseFolder(release.RoadieId, Configuration.LibraryFolder,
                    isReadOnly, release);
                await UpdateReleaseRank(release.Id);
                CacheManager.ClearRegion(release.CacheRegion);
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
                NewTracks = ReleaseFactory.AddedTrackIds.Count(),
                TimeSpanInSeconds = (int)sw.Elapsed.TotalSeconds
            });
            await DbContext.SaveChangesAsync();
            await LogAndPublish(
                $"ScanRelease `{release}`, By User `{user}`, WasDoneForInvalidTrackPlay [{wasDoneForInvalidTrackPlay}]",
                LogLevel.Information);
            return new OperationResult<bool>
            {
                IsSuccess = !errors.Any(),
                Data = true,
                OperationTime = sw.ElapsedMilliseconds,
                Errors = errors
            };
        }

        private void EventMessageLogger_Messages(object sender, EventMessage e)
        {
            Task.WaitAll(LogAndPublish(e.Message, e.Level));
        }

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

            await ScanActivityHub.Clients.All.SendAsync("SendSystemActivity", message);
        }

        private async Task<OperationResult<bool>> ScanFolder(DirectoryInfo d, DirectoryInfo dest, ApplicationUser user,
            bool isReadOnly)
        {
            var sw = new Stopwatch();
            sw.Start();

            long processedFiles = 0;
            await LogAndPublish($"** Processing Folder: [{d.FullName}]");

            long processedFolders = 0;
            var folderProcessor = new FolderProcessor(Configuration, HttpEncoder, Configuration.LibraryFolder,
                DbContext, CacheManager, MessageLogger, ArtistLookupEngine, ArtistFactory, ReleaseFactory, ImageFactory,
                ReleaseLookupEngine, AudioMetaDataHelper);

            var newArtists = 0;
            var newReleases = 0;
            var newTracks = 0;
            OperationResult<bool> result = null;
            foreach (var folder in Directory.EnumerateDirectories(d.FullName).ToArray())
            {
                result = await folderProcessor.Process(new DirectoryInfo(folder), isReadOnly);
                // Between folders flush cache, the caching for folder processing was intended for caching artist metadata lookups. Most of the time artists are in the same folder.
                CacheManager.Clear();
                processedFolders++;
            }

            if (result.AdditionalData != null)
            {
                newArtists = SafeParser.ToNumber<int>(result.AdditionalData["newArtists"]);
                newReleases = SafeParser.ToNumber<int>(result.AdditionalData["newReleases"]);
                newTracks = SafeParser.ToNumber<int>(result.AdditionalData["newTracks"]);
            }

            if (!isReadOnly) FolderProcessor.DeleteEmptyFolders(d, Logger);
            sw.Stop();
            DbContext.ScanHistories.Add(new data.ScanHistory
            {
                UserId = user.Id,
                NewArtists = newArtists,
                NewReleases = newReleases,
                NewTracks = newTracks,
                TimeSpanInSeconds = (int)sw.Elapsed.TotalSeconds
            });
            await DbContext.SaveChangesAsync();
            CacheManager.Clear();
            await LogAndPublish(
                $"**Completed!Processed Folders[{processedFolders}], Processed Files[{processedFiles}] : Elapsed Time[{sw.Elapsed}]");
            return new OperationResult<bool>
            {
                Data = true,
                IsSuccess = true,
                OperationTime = sw.ElapsedMilliseconds
            };
        }
    }
}