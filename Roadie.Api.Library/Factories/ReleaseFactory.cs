using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Imaging;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.Processors;
using Roadie.Library.SearchEngines.MetaData;
using Roadie.Library.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.Factories
{
#pragma warning disable EF1000

    public sealed class ReleaseFactory : FactoryBase, IReleaseFactory
    {
        private List<int> _addedTrackIds = new List<int>();

        public IEnumerable<int> AddedTrackIds
        {
            get
            {
                return this._addedTrackIds;
            }
        }

        private IAudioMetaDataHelper AudioMetaDataHelper { get; }

        private ILabelFactory LabelFactory { get; }

        public ReleaseFactory(IRoadieSettings configuration, IHttpEncoder httpEncoder, IRoadieDbContext context, ICacheManager cacheManager, ILogger logger, IArtistLookupEngine artistLookupEngine, ILabelFactory labelFactory, IAudioMetaDataHelper audioMetaDataHelper, IReleaseLookupEngine releaseLookupEngine)
            : base(configuration, context, cacheManager, logger, httpEncoder, artistLookupEngine, releaseLookupEngine)
        {
            this.LabelFactory = labelFactory;
            this.AudioMetaDataHelper = audioMetaDataHelper;
        }


        /// <summary>
        /// See if the given release has properties that have been modified that affect the folder structure, if so then handle necessary operations for changes
        /// </summary>
        /// <param name="release">Release that has been modified</param>
        /// <param name="oldReleaseFolder">Folder for release before any changes</param>
        /// <returns></returns>
        public async Task<OperationResult<bool>> CheckAndChangeReleaseTitle(Data.Release release, string oldReleaseFolder, string destinationFolder = null)
        {
            SimpleContract.Requires<ArgumentNullException>(release != null, "Invalid Release");
            SimpleContract.Requires<ArgumentNullException>(!string.IsNullOrEmpty(oldReleaseFolder), "Invalid Release Old Folder");

            destinationFolder = destinationFolder ?? this.Configuration.LibraryFolder;

            var sw = new Stopwatch();
            sw.Start();
            var now = DateTime.UtcNow;

            var result = false;
            var artistFolder = release.Artist.ArtistFileFolder(this.Configuration, destinationFolder);
            var newReleaseFolder = release.ReleaseFileFolder(artistFolder);
            if (!oldReleaseFolder.Equals(newReleaseFolder, StringComparison.OrdinalIgnoreCase))
            {
                this.Logger.LogTrace("Moving Release From Folder [{0}] To [{1}]", oldReleaseFolder, newReleaseFolder);

                // Create the new release folder
                if (!Directory.Exists(newReleaseFolder))
                {
                    Directory.CreateDirectory(newReleaseFolder);
                }
                var releaseDirectoryInfo = new DirectoryInfo(newReleaseFolder);
                // Update and move tracks under new release folder
                foreach (var releaseMedia in this.DbContext.ReleaseMedias.Where(x => x.ReleaseId == release.Id).ToArray())
                {
                    // Update the track path to have the new album title. This is needed because future scans might not work properly without updating track title.
                    foreach (var track in this.DbContext.Tracks.Where(x => x.ReleaseMediaId == releaseMedia.Id).ToArray())
                    {
                        var existingTrackPath = track.PathToTrack(this.Configuration, destinationFolder);

                        var existingTrackFileInfo = new FileInfo(existingTrackPath);
                        var newTrackFileInfo = new FileInfo(track.PathToTrack(this.Configuration, destinationFolder));
                        if (existingTrackFileInfo.Exists)
                        {
                            // Update the tracks release tags
                            var audioMetaData = await this.AudioMetaDataHelper.GetInfo(existingTrackFileInfo);
                            audioMetaData.Release = release.Title;
                            this.AudioMetaDataHelper.WriteTags(audioMetaData, existingTrackFileInfo);

                            // Update track path
                            track.FilePath = Path.Combine(releaseDirectoryInfo.Parent.Name, releaseDirectoryInfo.Name);
                            track.LastUpdated = now;

                            // Move the physical track
                            var newTrackPath = track.PathToTrack(this.Configuration, destinationFolder);
                            if (!existingTrackPath.Equals(newTrackPath, StringComparison.OrdinalIgnoreCase))
                            {
                                File.Move(existingTrackPath, newTrackPath);
                            }
                        }

                        this.CacheManager.ClearRegion(track.CacheRegion);
                    }
                }
                await this.DbContext.SaveChangesAsync();

                // Clean up any empty folders for the artist
                FolderPathHelper.DeleteEmptyFoldersForArtist(this.Configuration, release.Artist, destinationFolder);
            }

            sw.Stop();
            this.CacheManager.ClearRegion(release.CacheRegion);
            if (release.Artist != null)
            {
                this.CacheManager.ClearRegion(release.Artist.CacheRegion);
            }

            return new OperationResult<bool>
            {
                IsSuccess = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> Delete(Data.Release release, bool doDeleteFiles = false, bool doUpdateArtistCounts = true)
        {
            SimpleContract.Requires<ArgumentNullException>(release != null, "Invalid Release");
            SimpleContract.Requires<ArgumentNullException>(release.Artist != null, "Invalid Artist");

            var releaseCacheRegion = release.CacheRegion;
            var artistCacheRegion = release.Artist.CacheRegion;

            var result = false;
            var sw = new Stopwatch();
            sw.Start();
            if (doDeleteFiles)
            {
                var releaseTracks = (from r in this.DbContext.Releases
                                     join rm in this.DbContext.ReleaseMedias on r.Id equals rm.ReleaseId
                                     join t in this.DbContext.Tracks on rm.Id equals t.ReleaseMediaId
                                     where r.Id == release.Id
                                     select t).ToArray();
                foreach (var track in releaseTracks)
                {
                    string trackPath = null;
                    try
                    {
                        trackPath = track.PathToTrack(this.Configuration, this.Configuration.LibraryFolder);
                        if (File.Exists(trackPath))
                        {
                            File.Delete(trackPath);
                            this.Logger.LogWarning("x For Release [{0}], Deleted File [{1}]", release.Id, trackPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Logger.LogError(ex, string.Format("Error Deleting File [{0}] For Track [{1}] Exception [{2}]", trackPath, track.Id, ex.Serialize()));
                    }
                }
                try
                {
                    FolderPathHelper.DeleteEmptyFoldersForArtist(this.Configuration, release.Artist);
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex);
                }
            }
            var releaseLabelIds = this.DbContext.ReleaseLabels.Where(x => x.ReleaseId == release.Id).Select(x => x.LabelId).ToArray();
            this.DbContext.Releases.Remove(release);
            var i = await this.DbContext.SaveChangesAsync();
            result = true;
            try
            {
                this.CacheManager.ClearRegion(releaseCacheRegion);
                this.CacheManager.ClearRegion(artistCacheRegion);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, string.Format("Error Clearing Cache For Release [{0}] Exception [{1}]", release.Id, ex.Serialize()));
            }
            var now = DateTime.UtcNow;
            if (doUpdateArtistCounts)
            {
                await base.UpdateArtistCounts(release.Artist.Id, now);
            }
            if (releaseLabelIds != null && releaseLabelIds.Any())
            {
                foreach(var releaseLabelId in releaseLabelIds)
                {
                    await base.UpdateLabelCounts(releaseLabelId, now);
                }
            }
            sw.Stop();
            return new OperationResult<bool>
            {
                Data = result,
                IsSuccess = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<bool>> DeleteReleases(IEnumerable<Guid> releaseIds, bool doDeleteFiles = false)
        {
            SimpleContract.Requires<ArgumentNullException>(releaseIds != null && releaseIds.Any(), "No Release Ids Found");
            var result = false;
            var sw = new Stopwatch();
            sw.Start();

            var now = DateTime.UtcNow;
            var releases = (from r in this.DbContext.Releases.Include(r => r.Artist)
                            where releaseIds.Contains(r.RoadieId)
                            select r
                            ).ToArray();

            var artistIds = releases.Select(x => x.ArtistId).Distinct().ToArray();

            foreach (var release in releases)
            {
                var defaultResult = await this.Delete(release, doDeleteFiles, false);
                result = result & defaultResult.IsSuccess;
            }
            foreach(var artistId in artistIds)
            {
                await base.UpdateArtistCounts(artistId, now);
            }
            sw.Stop();

            return new OperationResult<bool>
            {
                Data = result,
                IsSuccess = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public OperationResult<Data.Release> GetAllForArtist(Data.Artist artist, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Merge one release into another one
        /// </summary>
        /// <param name="releaseToMerge">The release to be merged</param>
        /// <param name="releaseToMergeInto">The release to merge into</param>
        /// <param name="addAsMedia">If true then add a ReleaseMedia to the release to be merged into</param>
        /// <returns></returns>
        public async Task<OperationResult<bool>> MergeReleases(Data.Release releaseToMerge, Data.Release releaseToMergeInto, bool addAsMedia)
        {
            SimpleContract.Requires<ArgumentNullException>(releaseToMerge != null, "Invalid Release");
            SimpleContract.Requires<ArgumentNullException>(releaseToMergeInto != null, "Invalid Release");
            SimpleContract.Requires<ArgumentNullException>(releaseToMerge.Artist != null, "Invalid Artist");
            SimpleContract.Requires<ArgumentNullException>(releaseToMergeInto.Artist != null, "Invalid Artist");

            var result = false;

            var sw = new Stopwatch();
            sw.Start();

            var mergedFilesToDelete = new List<string>();
            var mergedTracksToMove = new List<Data.Track>();

            releaseToMergeInto.MediaCount = releaseToMergeInto.MediaCount ?? 0;

            var now = DateTime.UtcNow;
            var releaseToMergeReleaseMedia = this.DbContext.ReleaseMedias.Where(x => x.ReleaseId == releaseToMerge.Id).ToList();
            var releaseToMergeIntoReleaseMedia = this.DbContext.ReleaseMedias.Where(x => x.ReleaseId == releaseToMergeInto.Id).ToList();
            var releaseToMergeIntoLastMediaNumber = releaseToMergeIntoReleaseMedia.Max(x => x.MediaNumber);

            // Add new ReleaseMedia
            if (addAsMedia || !releaseToMergeIntoReleaseMedia.Any())
            {
                foreach (var rm in releaseToMergeReleaseMedia)
                {
                    releaseToMergeIntoLastMediaNumber++;
                    rm.ReleaseId = releaseToMergeInto.Id;
                    rm.MediaNumber = releaseToMergeIntoLastMediaNumber;
                    rm.LastUpdated = now;
                    releaseToMergeInto.MediaCount++;
                    releaseToMergeInto.TrackCount += rm.TrackCount;
                }
            }
            // Merge into existing ReleaseMedia
            else
            {
                // See if each media exists and merge details of each including tracks
                foreach (var rm in releaseToMergeReleaseMedia)
                {
                    var existingReleaseMedia = releaseToMergeIntoReleaseMedia.FirstOrDefault(x => x.MediaNumber == rm.MediaNumber);
                    var mergeTracks = this.DbContext.Tracks.Where(x => x.ReleaseMediaId == rm.Id).ToArray();
                    if (existingReleaseMedia == null)
                    {
                        releaseToMergeIntoLastMediaNumber++;
                        // Doesnt exist in release being merged to add
                        rm.ReleaseId = releaseToMergeInto.Id;
                        rm.MediaNumber = releaseToMergeIntoLastMediaNumber;
                        rm.LastUpdated = now;
                        releaseToMergeInto.MediaCount++;
                        releaseToMergeInto.TrackCount += rm.TrackCount;
                        mergedTracksToMove.AddRange(mergeTracks);
                    }
                    else
                    {
                        // ReleaseMedia Does exist merge tracks and details

                        var mergeIntoTracks = this.DbContext.Tracks.Where(x => x.ReleaseMediaId == existingReleaseMedia.Id).ToArray();
                        foreach (var mergeTrack in mergeTracks)
                        {
                            var existingTrack = mergeIntoTracks.FirstOrDefault(x => x.TrackNumber == mergeTrack.TrackNumber);
                            if (existingTrack == null)
                            {
                                // Track does not exist, update to existing ReleaseMedia and update ReleaseToMergeInfo counts
                                mergeTrack.LastUpdated = now;
                                mergeTrack.ReleaseMediaId = existingReleaseMedia.Id;
                                existingReleaseMedia.TrackCount++;
                                existingReleaseMedia.LastUpdated = now;
                                releaseToMergeInto.TrackCount++;
                            }
                            else
                            {
                                // Track does exist merge two tracks together
                                existingTrack.MusicBrainzId = existingTrack.MusicBrainzId ?? mergeTrack.MusicBrainzId;
                                existingTrack.SpotifyId = existingTrack.SpotifyId ?? mergeTrack.SpotifyId;
                                existingTrack.AmgId = existingTrack.AmgId ?? mergeTrack.AmgId;
                                existingTrack.ISRC = existingTrack.ISRC ?? mergeTrack.ISRC;
                                existingTrack.AmgId = existingTrack.AmgId ?? mergeTrack.AmgId;
                                existingTrack.LastFMId = existingTrack.LastFMId ?? mergeTrack.LastFMId;
                                existingTrack.PartTitles = existingTrack.PartTitles ?? mergeTrack.PartTitles;
                                existingTrack.PlayedCount = (existingTrack.PlayedCount ?? 0) + (mergeTrack.PlayedCount ?? 0);
                                if (mergeTrack.LastPlayed.HasValue && existingTrack.LastPlayed.HasValue && mergeTrack.LastPlayed > existingTrack.LastPlayed)
                                {
                                    existingTrack.LastPlayed = mergeTrack.LastPlayed;
                                }
                                existingTrack.Thumbnail = existingTrack.Thumbnail ?? mergeTrack.Thumbnail;
                                existingTrack.MusicBrainzId = existingTrack.MusicBrainzId ?? mergeTrack.MusicBrainzId;
                                existingTrack.Tags = existingTrack.Tags.AddToDelimitedList(mergeTrack.Tags.ToListFromDelimited());
                                if (!mergeTrack.Title.Equals(existingTrack.Title, StringComparison.OrdinalIgnoreCase))
                                {
                                    existingTrack.AlternateNames = existingTrack.AlternateNames.AddToDelimitedList(new string[] { mergeTrack.Title, mergeTrack.Title.ToAlphanumericName() });
                                }
                                existingTrack.AlternateNames = existingTrack.AlternateNames.AddToDelimitedList(mergeTrack.AlternateNames.ToListFromDelimited());
                                existingTrack.LastUpdated = now;
                                var mergedTrackFileName = mergeTrack.PathToTrack(this.Configuration, this.Configuration.LibraryFolder);
                                var trackFileName = existingTrack.PathToTrack(this.Configuration, this.Configuration.LibraryFolder);
                                if (!trackFileName.Equals(mergedTrackFileName, StringComparison.Ordinal) && File.Exists(trackFileName))
                                {
                                    mergedFilesToDelete.Add(mergedTrackFileName);
                                }
                            }
                        }
                    }
                }
            }

            var destinationRoot = this.Configuration.LibraryFolder;
            var releaseToMergeFolder = releaseToMerge.ReleaseFileFolder(releaseToMerge.Artist.ArtistFileFolder(this.Configuration, destinationRoot));
            var releaseToMergeIntoArtistFolder = releaseToMergeInto.Artist.ArtistFileFolder(this.Configuration, destinationRoot);
            var releaseToMergeIntoDirectory = new DirectoryInfo(releaseToMergeInto.ReleaseFileFolder(releaseToMergeIntoArtistFolder));

            // Move tracks for releaseToMergeInto into correct folders
            if (mergedTracksToMove.Any())
            {
                foreach (var track in mergedTracksToMove)
                {
                    var oldTrackPath = track.PathToTrack(this.Configuration, this.Configuration.LibraryFolder);
                    var newTrackPath = FolderPathHelper.TrackFullPath(this.Configuration, releaseToMerge.Artist, releaseToMerge, track);
                    var trackFile = new FileInfo(oldTrackPath);
                    if (!newTrackPath.ToLower().Equals(oldTrackPath.ToLower()))
                    {
                        var audioMetaData = await this.AudioMetaDataHelper.GetInfo(trackFile, false);
                        track.FilePath = FolderPathHelper.TrackPath(this.Configuration, releaseToMergeInto.Artist, releaseToMergeInto, track);
                        track.Hash = HashHelper.CreateMD5(releaseToMergeInto.ArtistId.ToString() + trackFile.LastWriteTimeUtc.GetHashCode().ToString() + audioMetaData.GetHashCode().ToString());
                        track.LastUpdated = now;
                        File.Move(oldTrackPath, newTrackPath);
                    }
                }
            }

            // Cleanup folders
            FolderProcessor.DeleteEmptyFolders(new DirectoryInfo(releaseToMergeIntoArtistFolder), this.Logger);

            // Now Merge release details
            releaseToMergeInto.AlternateNames = releaseToMergeInto.AlternateNames.AddToDelimitedList(new string[] { releaseToMerge.Title, releaseToMerge.Title.ToAlphanumericName() });
            releaseToMergeInto.AlternateNames = releaseToMergeInto.AlternateNames.AddToDelimitedList(releaseToMerge.AlternateNames.ToListFromDelimited());
            releaseToMergeInto.Tags = releaseToMergeInto.Tags.AddToDelimitedList(releaseToMerge.Tags.ToListFromDelimited());
            releaseToMergeInto.URLs.AddToDelimitedList(releaseToMerge.URLs.ToListFromDelimited());
            releaseToMergeInto.MusicBrainzId = releaseToMergeInto.MusicBrainzId ?? releaseToMerge.MusicBrainzId;
            releaseToMergeInto.Profile = releaseToMergeInto.Profile ?? releaseToMerge.Profile;
            releaseToMergeInto.ReleaseDate = releaseToMergeInto.ReleaseDate ?? releaseToMerge.ReleaseDate;
            releaseToMergeInto.MusicBrainzId = releaseToMergeInto.MusicBrainzId ?? releaseToMerge.MusicBrainzId;
            releaseToMergeInto.DiscogsId = releaseToMergeInto.DiscogsId ?? releaseToMerge.DiscogsId;
            releaseToMergeInto.ITunesId = releaseToMergeInto.ITunesId ?? releaseToMerge.ITunesId;
            releaseToMergeInto.AmgId = releaseToMergeInto.AmgId ?? releaseToMerge.AmgId;
            releaseToMergeInto.LastFMId = releaseToMergeInto.LastFMId ?? releaseToMerge.LastFMId;
            releaseToMergeInto.LastFMSummary = releaseToMergeInto.LastFMSummary ?? releaseToMerge.LastFMSummary;
            releaseToMergeInto.SpotifyId = releaseToMergeInto.SpotifyId ?? releaseToMerge.SpotifyId;
            releaseToMergeInto.Thumbnail = releaseToMergeInto.Thumbnail ?? releaseToMerge.Thumbnail;
            if (releaseToMergeInto.ReleaseType == ReleaseType.Unknown && releaseToMerge.ReleaseType != ReleaseType.Unknown)
            {
                releaseToMergeInto.ReleaseType = releaseToMerge.ReleaseType;
            }
            releaseToMergeInto.LastUpdated = now;
            await this.DbContext.SaveChangesAsync();

            // Update any collection pointers for release to be merged
            var collectionRecords = this.DbContext.CollectionReleases.Where(x => x.ReleaseId == releaseToMerge.Id);
            if (collectionRecords != null && collectionRecords.Any())
            {
                foreach (var cr in collectionRecords)
                {
                    cr.ReleaseId = releaseToMergeInto.Id;
                    cr.LastUpdated = now;
                }
                await this.DbContext.SaveChangesAsync();
            }

            // Update any existing playlist for release to be merged
            var playListTrackInfos = (from pl in this.DbContext.PlaylistTracks
                                      join t in this.DbContext.Tracks on pl.TrackId equals t.Id
                                      join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                      where rm.ReleaseId == releaseToMerge.Id
                                      select new
                                      {
                                          track = t,
                                          rm = rm,
                                          pl = pl
                                      }).ToArray();
            if (playListTrackInfos != null && playListTrackInfos.Any())
            {
                foreach (var playListTrackInfo in playListTrackInfos)
                {
                    var matchingTrack = (from t in this.DbContext.Tracks
                                         join rm in this.DbContext.ReleaseMedias on t.ReleaseMediaId equals rm.Id
                                         where rm.ReleaseId == releaseToMergeInto.Id
                                         where rm.MediaNumber == playListTrackInfo.rm.MediaNumber
                                         where t.TrackNumber == playListTrackInfo.track.TrackNumber
                                         select t).FirstOrDefault();
                    if (matchingTrack != null)
                    {
                        playListTrackInfo.pl.TrackId = matchingTrack.Id;
                        playListTrackInfo.pl.LastUpdated = now;
                    }
                }
                await this.DbContext.SaveChangesAsync();
            }

            await this.Delete(releaseToMerge);

            // Delete any files flagged to be deleted (duplicate as track already exists on merged to release)
            if (mergedFilesToDelete.Any())
            {
                foreach (var mergedFileToDelete in mergedFilesToDelete)
                {
                    try
                    {
                        if (File.Exists(mergedFileToDelete))
                        {
                            File.Delete(mergedFileToDelete);
                            this.Logger.LogWarning("x Deleted Merged File [{0}]", mergedFileToDelete);
                        }
                    }
                    catch
                    {
                    }
                }
            }

            // Clear cache regions for manipulated records
            this.CacheManager.ClearRegion(releaseToMergeInto.CacheRegion);
            if (releaseToMergeInto.Artist != null)
            {
                this.CacheManager.ClearRegion(releaseToMergeInto.Artist.CacheRegion);
            }
            if (releaseToMerge.Artist != null)
            {
                this.CacheManager.ClearRegion(releaseToMerge.Artist.CacheRegion);
            }

            sw.Stop();
            return new OperationResult<bool>
            {
                Data = result,
                IsSuccess = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }


        /// <summary>
        /// For the given ReleaseId, Scan folder adding new, removing not found and updating DB tracks for tracks found
        /// </summary>
        public async Task<OperationResult<bool>> ScanReleaseFolder(Guid releaseId, string destinationFolder, bool doJustInfo, Data.Release releaseToScan = null)
        {
            SimpleContract.Requires<ArgumentOutOfRangeException>((releaseId != Guid.Empty && releaseToScan == null) || releaseToScan != null, "Invalid ReleaseId");

            this._addedTrackIds.Clear();

            var result = false;
            var resultErrors = new List<Exception>();
            var sw = new Stopwatch();
            sw.Start();
            var modifiedRelease = false;
            string releasePath = null;
            try
            {
                var release = releaseToScan ?? this.DbContext.Releases
                                                             .Include(x => x.Artist)
                                                             .Include(x => x.Labels)
                                                             .FirstOrDefault(x => x.RoadieId == releaseId);
                if (release == null)
                {
                    this.Logger.LogCritical("Unable To Find Release [{0}]", releaseId);
                    return new OperationResult<bool>();
                }
                // This is recorded from metadata and if set then used to gauage if the release is complete
                short? totalTrackCount = null;
                short totalMissingCount = 0;
                releasePath = release.ReleaseFileFolder(release.Artist.ArtistFileFolder(this.Configuration, destinationFolder));
                var releaseDirectory = new DirectoryInfo(releasePath);
                if (!Directory.Exists(releasePath))
                {
                    this.Logger.LogWarning("Unable To Find Release Folder [{0}] For Release `{1}`", releasePath, release.ToString());
                }
                var now = DateTime.UtcNow;

                #region Get Tracks for Release from DB and set as missing any not found in Folder

                foreach (var releaseMedia in this.DbContext.ReleaseMedias.Where(x => x.ReleaseId == release.Id).ToArray())
                {
                    var foundMissingTracks = false;
                    foreach (var existingTrack in this.DbContext.Tracks.Where(x => x.ReleaseMediaId == releaseMedia.Id).ToArray())
                    {
                        var trackPath = existingTrack.PathToTrack(this.Configuration, destinationFolder);

                        if (!File.Exists(trackPath))
                        {
                            this.Logger.LogWarning("Track `{0}`, File [{1}] Not Found.", existingTrack.ToString(), trackPath);
                            if (!doJustInfo)
                            {
                                existingTrack.UpdateTrackMissingFile(now);
                                foundMissingTracks = true;
                                modifiedRelease = true;
                                totalMissingCount++;
                            }
                        }
                    }
                    if (foundMissingTracks)
                    {
                        await this.DbContext.SaveChangesAsync();
                    }
                }

                #endregion Get Tracks for Release from DB and set as missing any not found in Folder

                #region Scan Folder and Add or Update Existing Tracks from Files

                var existingReleaseMedia = this.DbContext.ReleaseMedias.Include(x => x.Tracks).Where(x => x.ReleaseId == release.Id).ToList();
                var foundInFolderTracks = new List<Data.Track>();
                short totalNumberOfTracksFound = 0;
                // This is the number of tracks metadata says the release should have (releaseMediaNumber, TotalNumberOfTracks)
                Dictionary<short, short?> releaseMediaTotalNumberOfTracks = new Dictionary<short, short?>();
                Dictionary<int, short> releaseMediaTracksFound = new Dictionary<int, short>();
                if (Directory.Exists(releasePath))
                {
                    foreach (var file in releaseDirectory.GetFiles("*.mp3", SearchOption.AllDirectories))
                    {
                        int? trackArtistId = null;
                        string partTitles = null;
                        var audioMetaData = await this.AudioMetaDataHelper.GetInfo(file, doJustInfo);
                        // This is the path for the new track not in the database but the found MP3 file to be added to library
                        var trackPath = Path.Combine(releaseDirectory.Parent.Name, releaseDirectory.Name);

                        if (audioMetaData.IsValid)
                        {
                            var trackHash = HashHelper.CreateMD5(release.ArtistId.ToString() + file.LastWriteTimeUtc.GetHashCode().ToString() + audioMetaData.GetHashCode().ToString());
                            totalNumberOfTracksFound++;
                            totalTrackCount = totalTrackCount ?? (short)(audioMetaData.TotalTrackNumbers ?? 0);
                            var releaseMediaNumber = (short)(audioMetaData.Disk ?? 1);
                            if (!releaseMediaTotalNumberOfTracks.ContainsKey(releaseMediaNumber))
                            {
                                releaseMediaTotalNumberOfTracks.Add(releaseMediaNumber, (short)(audioMetaData.TotalTrackNumbers ?? 0));
                            }
                            else
                            {
                                releaseMediaTotalNumberOfTracks[releaseMediaNumber] = releaseMediaTotalNumberOfTracks[releaseMediaNumber].TakeLarger((short)(audioMetaData.TotalTrackNumbers ?? 0));
                            }
                            var releaseMedia = existingReleaseMedia.FirstOrDefault(x => x.MediaNumber == releaseMediaNumber);
                            if (releaseMedia == null)
                            {
                                // New ReleaseMedia - Not Found In Database
                                releaseMedia = new ReleaseMedia
                                {
                                    ReleaseId = release.Id,
                                    Status = Statuses.Incomplete,
                                    MediaNumber = releaseMediaNumber
                                };
                                this.DbContext.ReleaseMedias.Add(releaseMedia);
                                await this.DbContext.SaveChangesAsync();
                                existingReleaseMedia.Add(releaseMedia);
                                modifiedRelease = true;
                            }
                            else
                            {
                                // Existing ReleaseMedia Found
                                releaseMedia.LastUpdated = now;
                            }
                            var track = releaseMedia.Tracks.FirstOrDefault(x => x.TrackNumber == audioMetaData.TrackNumber);
                            if (track == null)
                            {
                                // New Track - Not Found In Database
                                track = new Data.Track
                                {
                                    Status = Statuses.New,
                                    FilePath = trackPath,
                                    FileName = file.Name,
                                    FileSize = (int)file.Length,
                                    Hash = trackHash,
                                    MusicBrainzId = audioMetaData.MusicBrainzId,
                                    AmgId = audioMetaData.AmgId,
                                    SpotifyId = audioMetaData.SpotifyId,
                                    Title = audioMetaData.Title,
                                    TrackNumber = audioMetaData.TrackNumber ?? totalNumberOfTracksFound,
                                    Duration = audioMetaData.Time != null ? (int)audioMetaData.Time.Value.TotalMilliseconds : 0,
                                    ReleaseMediaId = releaseMedia.Id,
                                    ISRC = audioMetaData.ISRC,
                                    LastFMId = audioMetaData.LastFmId,
                                };

                                if (audioMetaData.TrackArtist != null)
                                {
                                    if (audioMetaData.TrackArtists.Count() == 1)
                                    {
                                        var trackArtistData = await this.ArtistLookupEngine.GetByName(new AudioMetaData { Artist = audioMetaData.TrackArtist }, true);
                                        if (trackArtistData.IsSuccess && release.ArtistId != trackArtistData.Data.Id)
                                        {
                                            trackArtistId = trackArtistData.Data.Id;
                                        }
                                    }
                                    else if (audioMetaData.TrackArtists.Any())
                                    {
                                        partTitles = string.Join(AudioMetaData.ArtistSplitCharacter.ToString(), audioMetaData.TrackArtists);
                                    }
                                    else
                                    {
                                        partTitles = audioMetaData.TrackArtist;
                                    }
                                }
                                var alt = track.Title.ToAlphanumericName();
                                track.AlternateNames = !alt.Equals(audioMetaData.Title, StringComparison.OrdinalIgnoreCase) ? track.AlternateNames.AddToDelimitedList(new string[] { alt }) : null;
                                track.ArtistId = trackArtistId;
                                track.PartTitles = partTitles;
                                this.DbContext.Tracks.Add(track);
                                await this.DbContext.SaveChangesAsync();
                                this._addedTrackIds.Add(track.Id);
                                modifiedRelease = true;
                            }
                            else if (string.IsNullOrEmpty(track.Hash) || trackHash != track.Hash)
                            {
                                if (audioMetaData.TrackArtist != null)
                                {
                                    if (audioMetaData.TrackArtists.Count() == 1)
                                    {
                                        var trackArtistData = await this.ArtistLookupEngine.GetByName(new AudioMetaData { Artist = audioMetaData.TrackArtist }, true);
                                        if (trackArtistData.IsSuccess && release.ArtistId != trackArtistData.Data.Id)
                                        {
                                            trackArtistId = trackArtistData.Data.Id;
                                        }
                                    }
                                    else if (audioMetaData.TrackArtists.Any())
                                    {
                                        partTitles = string.Join(AudioMetaData.ArtistSplitCharacter.ToString(), audioMetaData.TrackArtists);
                                    }
                                    else
                                    {
                                        partTitles = audioMetaData.TrackArtist;
                                    }
                                }
                                track.Title = audioMetaData.Title;
                                track.Duration = audioMetaData.Time != null ? (int)audioMetaData.Time.Value.TotalMilliseconds : 0;
                                track.TrackNumber = audioMetaData.TrackNumber ?? totalNumberOfTracksFound;
                                track.ArtistId = trackArtistId;
                                track.PartTitles = partTitles;
                                track.Hash = trackHash;
                                track.FileName = file.Name;
                                track.FileSize = (int)file.Length;
                                track.FilePath = trackPath;
                                track.Status = Statuses.Ok;
                                track.LastUpdated = now;
                                var alt = track.Title.ToAlphanumericName();
                                if (!alt.Equals(track.Title, StringComparison.OrdinalIgnoreCase))
                                {
                                    track.AlternateNames = track.AlternateNames.AddToDelimitedList(new string[] { alt });
                                }
                                track.TrackNumber = audioMetaData.TrackNumber ?? -1;
                                track.LastUpdated = now;
                                modifiedRelease = true;
                            }
                            else if (track.Status != Statuses.Ok)
                            {
                                track.Status = Statuses.Ok;
                                track.LastUpdated = now;
                                modifiedRelease = true;
                            }
                            foundInFolderTracks.Add(track);
                            if (releaseMediaTracksFound.ContainsKey(releaseMedia.Id))
                            {
                                releaseMediaTracksFound[releaseMedia.Id]++;
                            }
                            else
                            {
                                releaseMediaTracksFound[releaseMedia.Id] = 1;
                            }
                        }
                        else
                        {
                            this.Logger.LogWarning("Release Track File Has Invalid MetaData `{0}`", audioMetaData.ToString());
                        }
                    }
                }
                else
                {
                    this.Logger.LogWarning("Unable To Find Releaes Path [{0}] For Release `{1}`", releasePath, release.ToString());
                }
                var releaseMediaNumbersFound = new List<short?>();
                foreach (var kp in releaseMediaTracksFound)
                {
                    var releaseMedia = this.DbContext.ReleaseMedias.FirstOrDefault(x => x.Id == kp.Key);
                    if (releaseMedia != null)
                    {
                        if (!releaseMediaNumbersFound.Any(x => x == releaseMedia.MediaNumber))
                        {
                            releaseMediaNumbersFound.Add(releaseMedia.MediaNumber);
                        }
                        var releaseMediaFoundInFolderTrackNumbers = foundInFolderTracks.Where(x => x.ReleaseMediaId == releaseMedia.Id).Select(x => x.TrackNumber).OrderBy(x => x).ToArray();
                        var areTracksForRelaseMediaSequential = releaseMediaFoundInFolderTrackNumbers.Zip(releaseMediaFoundInFolderTrackNumbers.Skip(1), (a, b) => (a + 1) == b).All(x => x);
                        if (!areTracksForRelaseMediaSequential)
                        {
                            this.Logger.LogDebug("ReleaseMedia [{0}] Track Numbers Are Not Sequential", releaseMedia.Id);
                        }
                        releaseMedia.TrackCount = kp.Value;
                        releaseMedia.LastUpdated = now;
                        releaseMedia.Status = areTracksForRelaseMediaSequential ? Statuses.Ok : Statuses.Incomplete;
                        await this.DbContext.SaveChangesAsync();
                        modifiedRelease = true;
                    };
                }
                var foundInFolderTrackNumbers = foundInFolderTracks.Select(x => x.TrackNumber).OrderBy(x => x).ToArray();
                if (modifiedRelease || !foundInFolderTrackNumbers.Count().Equals(release.TrackCount) || releaseMediaNumbersFound.Count() != (release.MediaCount ?? 0))
                {
                    var areTracksForRelaseSequential = foundInFolderTrackNumbers.Zip(foundInFolderTrackNumbers.Skip(1), (a, b) => (a + 1) == b).All(x => x);
                    var maxFoundInFolderTrackNumbers = foundInFolderTrackNumbers.Any() ? (short)foundInFolderTrackNumbers.Max() : (short)0;
                    release.Status = areTracksForRelaseSequential ? Statuses.Ok : Statuses.Incomplete;
                    release.TrackCount = (short)foundInFolderTrackNumbers.Count();
                    release.MediaCount = (short)releaseMediaNumbersFound.Count();
                    if (release.TrackCount < maxFoundInFolderTrackNumbers)
                    {
                        release.TrackCount = maxFoundInFolderTrackNumbers;
                    }
                    release.LibraryStatus = release.TrackCount > 0 && release.TrackCount == totalNumberOfTracksFound ? LibraryStatus.Complete : LibraryStatus.Incomplete;
                    release.LastUpdated = now;
                    release.Status = release.LibraryStatus == LibraryStatus.Complete ? Statuses.Complete : Statuses.Incomplete;

                    await this.DbContext.SaveChangesAsync();
                    this.CacheManager.ClearRegion(release.Artist.CacheRegion);
                    this.CacheManager.ClearRegion(release.CacheRegion);
                }

                #endregion Scan Folder and Add or Update Existing Tracks from Files

                if (release.Thumbnail == null)
                {
                    var imageFiles = ImageHelper.ImageFilesInFolder(releasePath, SearchOption.AllDirectories);
                    if (imageFiles != null && imageFiles.Any())
                    {
                        foreach (var imageFile in imageFiles)
                        {
                            var i = new FileInfo(imageFile);
                            var iName = i.Name.ToLower().Trim();
                            var isCoverArtType = iName.Contains("cover") || iName.Contains("folder") || iName.Contains("front") || iName.Contains("release") || iName.Contains("album");
                            if (isCoverArtType)
                            {
                                // Read image and convert to jpeg
                                release.Thumbnail = File.ReadAllBytes(i.FullName);
                                release.Thumbnail = ImageHelper.ResizeImage(release.Thumbnail, this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height);
                                release.Thumbnail = ImageHelper.ConvertToJpegFormat(release.Thumbnail);
                                release.LastUpdated = now;
                                await this.DbContext.SaveChangesAsync();
                                this.CacheManager.ClearRegion(release.Artist.CacheRegion);
                                this.CacheManager.ClearRegion(release.CacheRegion);
                                this.Logger.LogInformation("Update Thumbnail using Release Cover File [{0}]", iName);
                                break;
                            }
                        }
                    }
                }

                sw.Stop();


                await base.UpdateReleaseCounts(release.Id, now);
                await base.UpdateArtistCountsForRelease(release.Id, now);
                if(release.Labels != null && release.Labels.Any())
                {
                    foreach(var label in release.Labels)
                    {
                        await base.UpdateLabelCounts(label.Id, now);
                    }
                }

                this.Logger.LogInformation("Scanned Release `{0}` Folder [{1}], Modified Release [{2}], OperationTime [{3}]", release.ToString(), releasePath, modifiedRelease, sw.ElapsedMilliseconds);
                result = true;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "ReleasePath [" + releasePath + "] " + ex.Serialize());
                resultErrors.Add(ex);
            }
            return new OperationResult<bool>
            {
                Data = result,
                IsSuccess = result,
                Errors = resultErrors,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        [Obsolete]
        public async Task<OperationResult<Data.Release>> Update(Data.Release release, IEnumerable<Data.Image> releaseImages, string originalReleaseFolder, string destinationFolder = null)
        {
            SimpleContract.Requires<ArgumentNullException>(release != null, "Invalid Release");

            var sw = new Stopwatch();
            sw.Start();

            var artistFolder = release.Artist.ArtistFileFolder(this.Configuration, destinationFolder ?? this.Configuration.LibraryFolder);
            var releaseGenreTables = release.Genres.Select(x => new ReleaseGenre { ReleaseId = release.Id, GenreId = x.GenreId }).ToList();
            var releaseLabels = release.Labels.Select(x => new Data.ReleaseLabel
            {
                CatalogNumber = x.CatalogNumber,
                BeginDate = x.BeginDate,
                EndDate = x.EndDate,
                ReleaseId = release.Id,
                LabelId = x.Label != null && x.Label.Id > 0 ? x.Label.Id : x.LabelId,
                Status = x.Status,
                RoadieId = x.RoadieId
            }).ToList();
            var result = true;

            var now = DateTime.UtcNow;

            release.LastUpdated = now;
            release.Labels = null;

            await this.CheckAndChangeReleaseTitle(release, originalReleaseFolder);

            await this.DbContext.SaveChangesAsync();

            this.DbContext.ReleaseGenres.RemoveRange((from at in this.DbContext.ReleaseGenres
                                                      where at.ReleaseId == release.Id
                                                      select at));
            release.Genres = releaseGenreTables;

            var existingReleaseLabelIds = (from rl in releaseLabels
                                           where rl.Status != Statuses.New
                                           select rl.RoadieId).ToArray();
            this.DbContext.ReleaseLabels.RemoveRange((from rl in this.DbContext.ReleaseLabels
                                                      where rl.ReleaseId == release.Id
                                                      where !(from x in existingReleaseLabelIds select x).Contains(rl.RoadieId)
                                                      select rl));
            release.Labels = releaseLabels;
            await this.DbContext.SaveChangesAsync();

            if (releaseImages != null)
            {
                var existingImageIds = (from ai in releaseImages
                                        where ai.Status != Statuses.New
                                        select ai.RoadieId).ToArray();
                this.DbContext.Images.RemoveRange((from i in this.DbContext.Images
                                                   where i.ReleaseId == release.Id
                                                   where !(from x in existingImageIds select x).Contains(i.RoadieId)
                                                   select i));
                await this.DbContext.SaveChangesAsync();
                if (releaseImages.Any(x => x.Status == Statuses.New))
                {
                    foreach (var releaseImage in releaseImages.Where(x => x.Status == Statuses.New))
                    {
                        this.DbContext.Images.Add(releaseImage);
                    }
                    try
                    {
                        await this.DbContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        this.Logger.LogError(ex);
                    }
                }
            }

            this.CacheManager.ClearRegion(release.CacheRegion);
            if (release.Artist != null)
            {
                this.CacheManager.ClearRegion(release.Artist.CacheRegion);
            }
            sw.Stop();

            return new OperationResult<Data.Release>
            {
                Data = release,
                IsSuccess = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }
    }
}