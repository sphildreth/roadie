using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Imaging;
using Roadie.Library.Logging;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.LastFm;
using Roadie.Library.MetaData.MusicBrainz;
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
    public sealed class ReleaseFactory : FactoryBase
    {
        public const string CoverFilename = "cover.jpg";

        private readonly LabelFactory _labelFactory = null;
        private List<int> _addedReleaseIds = new List<int>();
        private List<int> _addedTrackIds = new List<int>();
        private ArtistFactory _artistFactory = null;
        private AudioMetaDataHelper _audioMetaDataHelper = null;

        public IEnumerable<int> AddedReleaseIds
        {
            get
            {
                return this._addedReleaseIds;
            }
        }

        public IEnumerable<int> AddedTrackIds
        {
            get
            {
                return this._addedTrackIds;
            }
        }

        private ArtistFactory ArtistFactory
        {
            get
            {
                return this._artistFactory;
            }
        }

        private AudioMetaDataHelper AudioMetaDataHelper
        {
            get
            {
                return this._audioMetaDataHelper ?? (this._audioMetaDataHelper = new AudioMetaDataHelper(this.Configuration,
                                                                                                         this.HttpEncoder,
                                                                                                         this.DbContext,
                                                                                                         new MusicBrainzProvider(this.Configuration, this.CacheManager, this.Logger),
                                                                                                         new LastFmHelper(this.Configuration, this.CacheManager, this.Logger),
                                                                                                         this.CacheManager,
                                                                                                         this.Logger));
            }
            set
            {
                this._audioMetaDataHelper = value;
            }
        }

        private LabelFactory LabelFactory
        {
            get
            {
                return this._labelFactory;
            }
        }

        public ReleaseFactory(IRoadieSettings configuration, IHttpEncoder httpEncoder, IRoadieDbContext context, ICacheManager cacheManager, ILogger logger, LabelFactory labelFactory = null, ArtistFactory artistFactory = null) : base(configuration, context, cacheManager, logger, httpEncoder)
        {
            this._labelFactory = labelFactory ?? new LabelFactory(configuration, httpEncoder, context, CacheManager, logger);
            this._artistFactory = artistFactory ?? new ArtistFactory(configuration, httpEncoder, context, CacheManager, logger);
        }

        public async Task<FactoryResult<Data.Release>> Add(Data.Release release, bool doAddTracksInDatabase = false)
        {
            SimpleContract.Requires<ArgumentNullException>(release != null, "Invalid Release");

            try
            {
                var releaseGenreTables = release.Genres;
                var releaseImages = release.Images;
                var releaseMedias = release.Medias;
                var releaseLabels = release.Labels;
                var now = DateTime.UtcNow;
                release.AlternateNames = release.AlternateNames.AddToDelimitedList(new string[] { release.Title.ToAlphanumericName() });
                release.Images = null;
                release.Labels = null;
                release.Medias = null;
                release.LibraryStatus = LibraryStatus.Incomplete;
                release.Status = Statuses.New;
                if (!release.IsValid)
                {
                    return new FactoryResult<Data.Release>
                    {
                        Errors = new List<string> { "Release is Invalid" }
                    };
                }
                this.DbContext.Releases.Add(release);
                int inserted = 0;
                try
                {
                    inserted = await this.DbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    this.Logger.Error(ex, ex.Serialize());
                }
                if (inserted > 0 && release.Id > 0)
                {
                    this._addedReleaseIds.Add(release.Id);
                    if (releaseGenreTables != null && releaseGenreTables.Any(x => x.GenreId == null))
                    {
                        foreach (var releaseGenreTable in releaseGenreTables)
                        {
                            var genreName = releaseGenreTable.Genre.Name.ToLower().Trim();
                            if (string.IsNullOrEmpty(genreName))
                            {
                                continue;
                            }
                            var genre = this.DbContext.Genres.FirstOrDefault(x => x.Name.ToLower().Trim() == genreName);
                            if (genre == null)
                            {
                                genre = new Genre
                                {
                                    Name = releaseGenreTable.Genre.Name
                                };
                                this.DbContext.Genres.Add(genre);
                                await this.DbContext.SaveChangesAsync();
                            }
                            if (genre != null && genre.Id > 0)
                            {
                                string sql = null;
                                try
                                {
                                    sql = string.Format("INSERT INTO `releaseGenreTable` (releaseId, genreId) VALUES ({0}, {1});", release.Id, genre.Id);
                                    await this.DbContext.Database.ExecuteSqlCommandAsync(sql);
                                }
                                catch (Exception ex)
                                {
                                    this._logger.Error(ex, "Sql [" + sql + "]");
                                }
                            }
                        }
                    }
                    if (releaseImages != null && releaseImages.Any(x => x.Status == Statuses.New))
                    {
                        foreach (var releaseImage in releaseImages)
                        {
                            this.DbContext.Images.Add(new Data.Image
                            {
                                ReleaseId = release.Id,
                                Url = releaseImage.Url,
                                Signature = releaseImage.Signature,
                                Bytes = releaseImage.Bytes
                            });
                        }
                        try
                        {
                            await this.DbContext.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            this.Logger.Error(ex);
                        }
                    }

                    if (releaseLabels != null && releaseLabels.Any(x => x.Status == Statuses.New))
                    {
                        foreach (var neweleaseLabel in releaseLabels.Where(x => x.Status == Statuses.New))
                        {
                            var labelFetch = await this.LabelFactory.GetByName(neweleaseLabel.Label.Name, true);
                            if (labelFetch.IsSuccess)
                            {
                                this.DbContext.ReleaseLabels.Add(new Data.ReleaseLabel
                                {
                                    CatalogNumber = neweleaseLabel.CatalogNumber,
                                    BeginDate = neweleaseLabel.BeginDate,
                                    EndDate = neweleaseLabel.EndDate,
                                    ReleaseId = release.Id,
                                    LabelId = labelFetch.Data.Id
                                });
                            }
                        }
                        try
                        {
                            await this.DbContext.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            this.Logger.Error(ex);
                        }
                    }
                    if (doAddTracksInDatabase)
                    {
                        if (releaseMedias != null && releaseMedias.Any(x => x.Status == Statuses.New))
                        {
                            foreach (var newReleaseMedia in releaseMedias.Where(x => x.Status == Statuses.New))
                            {
                                var releasemedia = new Data.ReleaseMedia
                                {
                                    Status = Statuses.Incomplete,
                                    MediaNumber = newReleaseMedia.MediaNumber,
                                    SubTitle = newReleaseMedia.SubTitle,
                                    TrackCount = newReleaseMedia.TrackCount,
                                    ReleaseId = release.Id
                                };
                                var releasemediatracks = new List<Data.Track>();
                                foreach (var newTrack in newReleaseMedia.Tracks)
                                {
                                    int? trackArtistId = null;
                                    string partTitles = null;
                                    if (newTrack.TrackArtist != null)
                                    {
                                        if (!release.IsCastRecording)
                                        {
                                            var trackArtistData = await this.ArtistFactory.GetByName(new AudioMetaData { Artist = newTrack.TrackArtist.Name }, true);
                                            if (trackArtistData.IsSuccess)
                                            {
                                                trackArtistId = trackArtistData.Data.Id;
                                            }
                                        }
                                        else if (newTrack.TrackArtists != null && newTrack.TrackArtists.Any())
                                        {
                                            partTitles = string.Join("/", newTrack.TrackArtists);
                                        }
                                        else
                                        {
                                            partTitles = newTrack.TrackArtist.Name;
                                        }
                                    }
                                    releasemediatracks.Add(new Data.Track
                                    {
                                        ArtistId = trackArtistId,
                                        PartTitles = partTitles,
                                        Status = Statuses.Incomplete,
                                        TrackNumber = newTrack.TrackNumber,
                                        MusicBrainzId = newTrack.MusicBrainzId,
                                        SpotifyId = newTrack.SpotifyId,
                                        AmgId = newTrack.AmgId,
                                        Title = newTrack.Title,
                                        AlternateNames = newTrack.AlternateNames,
                                        Duration = newTrack.Duration,
                                        Tags = newTrack.Tags,
                                        ISRC = newTrack.ISRC,
                                        LastFMId = newTrack.LastFMId
                                    });
                                }
                                releasemedia.Tracks = releasemediatracks;
                                this.DbContext.ReleaseMedias.Add(releasemedia);
                            }
                            try
                            {
                                await this.DbContext.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                this.Logger.Error(ex);
                            }
                        }
                    }

                    this.Logger.Info("Added New Release: [{0}]", release.ToString());
                }
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex, ex.Serialize());
            }
            return new FactoryResult<Data.Release>
            {
                IsSuccess = release.Id > 0,
                Data = release
            };
        }

        /// <summary>
        /// See if the given release has properties that have been modified that affect the folder structure, if so then handle necessary operations for changes
        /// </summary>
        /// <param name="release">Release that has been modified</param>
        /// <param name="oldReleaseFolder">Folder for release before any changes</param>
        /// <returns></returns>
        public async Task<FactoryResult<bool>> CheckAndChangeReleaseTitle(Data.Release release, string oldReleaseFolder, string destinationFolder = null)
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
                this.Logger.Trace("Moving Release From Folder [{0}] To [{1}]", oldReleaseFolder, newReleaseFolder);

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

                        this._cacheManager.ClearRegion(track.CacheRegion);
                    }
                }
                await this.DbContext.SaveChangesAsync();

                // Clean up any empty folders for the artist
                FolderPathHelper.DeleteEmptyFoldersForArtist(this.Configuration, release.Artist, destinationFolder);
            }

            sw.Stop();
            this._cacheManager.ClearRegion(release.CacheRegion);
            if (release.Artist != null)
            {
                this._cacheManager.ClearRegion(release.Artist.CacheRegion);
            }

            return new FactoryResult<bool>
            {
                IsSuccess = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<FactoryResult<bool>> Delete(Data.Release release, bool doDeleteFiles = false)
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
                            this.Logger.Warning("x For Release [{0}], Deleted File [{1}]", release.Id, trackPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Logger.Error(ex, string.Format("Error Deleting File [{0}] For Track [{1}] Exception [{2}]", trackPath, track.Id, ex.Serialize()));
                    }
                }
                try
                {
                    FolderPathHelper.DeleteEmptyFoldersForArtist(this.Configuration, release.Artist);
                }
                catch (Exception ex)
                {
                    this.Logger.Error(ex);
                }
            }
            this.DbContext.Releases.Remove(release);
            var i = await this.DbContext.SaveChangesAsync();
            result = true;
            try
            {
                this._cacheManager.ClearRegion(releaseCacheRegion);
                this._cacheManager.ClearRegion(artistCacheRegion);
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex, string.Format("Error Clearing Cache For Release [{0}] Exception [{1}]", release.Id, ex.Serialize()));
            }
            sw.Stop();
            return new FactoryResult<bool>
            {
                Data = result,
                IsSuccess = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<FactoryResult<bool>> DeleteReleases(IEnumerable<Guid> releaseIds, bool doDeleteFiles = false)
        {
            SimpleContract.Requires<ArgumentNullException>(releaseIds != null && releaseIds.Any(), "No Release Ids Found");
            var result = false;
            var sw = new Stopwatch();
            sw.Start();

            var releases = (from r in this.DbContext.Releases.Include(r => r.Artist)
                            where releaseIds.Contains(r.RoadieId)
                            select r
                            ).ToArray();

            foreach (var release in releases)
            {
                var defaultResult = await this.Delete(release, doDeleteFiles);
                result = result & defaultResult.IsSuccess;
            }

            sw.Stop();

            return new FactoryResult<bool>
            {
                Data = result,
                IsSuccess = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public FactoryResult<Data.Release> GetAllForArtist(Data.Artist artist, bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        public async Task<FactoryResult<Data.Release>> GetByName(Data.Artist artist, AudioMetaData metaData, bool doFindIfNotInDatabase = false, bool doAddTracksInDatabase = false, int? submissionId = null)
        {
            SimpleContract.Requires<ArgumentNullException>(artist != null, "Invalid Artist");
            SimpleContract.Requires<ArgumentOutOfRangeException>(artist.Id > 0, "Invalid Artist Id");
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                var cacheRegion = (new Data.Release { Artist = artist, Title = metaData.Release }).CacheRegion;
                var cacheKey = string.Format("urn:release_by_artist_id_and_name:{0}:{1}", artist.Id, metaData.Release);
                var resultInCache = this.CacheManager.Get<Data.Release>(cacheKey, cacheRegion);
                if (resultInCache != null)
                {
                    sw.Stop();
                    return new FactoryResult<Data.Release>
                    {
                        IsSuccess = true,
                        OperationTime = sw.ElapsedMilliseconds,
                        Data = resultInCache
                    };
                }
                var getParams = new List<object>();
                var searchName = metaData.Release.NormalizeName().ToLower();
                var specialSearchName = metaData.Release.ToAlphanumericName();
                getParams.Add(new MySqlParameter("@artistId", artist.Id));
                getParams.Add(new MySqlParameter("@isTitle", searchName));
                getParams.Add(new MySqlParameter("@startAlt", string.Format("{0}|%", searchName)));
                getParams.Add(new MySqlParameter("@inAlt", string.Format("%|{0}|%", searchName)));
                getParams.Add(new MySqlParameter("@endAlt", string.Format("%|{0}", searchName)));
                getParams.Add(new MySqlParameter("@sstartAlt", string.Format("{0}|%", specialSearchName)));
                getParams.Add(new MySqlParameter("@sinAlt", string.Format("%|{0}|%", specialSearchName)));
                getParams.Add(new MySqlParameter("@sendAlt", string.Format("%|{0}", specialSearchName)));
                var release = this.DbContext.Releases.FromSql(@"SELECT *
                FROM `release`
                WHERE artistId = @artistId
                AND (LCASE(title) = @isTitle
                OR LCASE(alternatenames) = @isTitle
                OR alternatenames like @startAlt
                OR alternatenames like @sstartAlt
                OR alternatenames like @inAlt
                OR alternatenames like @sinAlt
                OR alternatenames like @endAlt
                OR alternatenames like @sendAlt)
                LIMIT 1;", getParams.ToArray()).FirstOrDefault();
                sw.Stop();
                if (release == null || !release.IsValid)
                {
                    this._logger.Info("ReleaseFactory: Release Not Found For Artist [{0}] MetaData [{1}]", artist.ToString(), metaData.ToString());
                    if (doFindIfNotInDatabase)
                    {
                        OperationResult<Data.Release> releaseSearch = new OperationResult<Data.Release>();
                        try
                        {
                            releaseSearch = await this.PerformMetaDataProvidersReleaseSearch(metaData, artist.ArtistFileFolder(this.Configuration, this.Configuration.LibraryFolder), submissionId);
                        }
                        catch (Exception ex)
                        {
                            sw.Stop();
                            this.Logger.Error(ex);
                            return new FactoryResult<Data.Release>
                            {
                                OperationTime = sw.ElapsedMilliseconds,
                                Errors = new List<string> { ex.ToString() }
                            };
                        }
                        if (releaseSearch.IsSuccess)
                        {
                            release = releaseSearch.Data;
                            release.ArtistId = artist.Id;
                            var addResult = await this.Add(release, doAddTracksInDatabase);
                            if (!addResult.IsSuccess)
                            {
                                sw.Stop();
                                return new FactoryResult<Data.Release>
                                {
                                    OperationTime = sw.ElapsedMilliseconds,
                                    Errors = addResult.Errors
                                };
                            }
                        }
                    }
                }
                if (release != null)
                {
                    this.CacheManager.Add(cacheKey, release);
                }
                return new FactoryResult<Data.Release>
                {
                    IsSuccess = release != null,
                    OperationTime = sw.ElapsedMilliseconds,
                    Data = release
                };
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex);
            }
            return new FactoryResult<Data.Release>();
        }

        /// <summary>
        /// Merge one release into another one
        /// </summary>
        /// <param name="releaseToMerge">The release to be merged</param>
        /// <param name="releaseToMergeInto">The release to merge into</param>
        /// <param name="addAsMedia">If true then add a ReleaseMedia to the release to be merged into</param>
        /// <returns></returns>
        public async Task<FactoryResult<bool>> MergeReleases(Data.Release releaseToMerge, Data.Release releaseToMergeInto, bool addAsMedia)
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
            var folderProcessor = new FolderProcessor(this.Configuration, this.HttpEncoder, destinationRoot, this.DbContext, this.CacheManager, this.Logger);
            folderProcessor.DeleteEmptyFolders(new DirectoryInfo(releaseToMergeIntoArtistFolder));

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
                            this.Logger.Warning("x Deleted Merged File [{0}]", mergedFileToDelete);
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
            return new FactoryResult<bool>
            {
                Data = result,
                IsSuccess = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        public async Task<OperationResult<Data.Release>> PerformMetaDataProvidersReleaseSearch(AudioMetaData metaData, string artistFolder = null, int? submissionId = null)
        {
            SimpleContract.Requires<ArgumentNullException>(metaData != null, "Invalid MetaData");

            var sw = new Stopwatch();
            sw.Start();

            var result = new Data.Release
            {
                Title = metaData.Release.ToTitleCase(false),
                TrackCount = (short)(metaData.TotalTrackNumbers ?? 0),
                ReleaseDate = SafeParser.ToDateTime(metaData.Year),
                SubmissionId = submissionId
            };
            var resultsExceptions = new List<Exception>();
            var releaseGenres = new List<string>();
            // Add any Genre found in the given MetaData
            if (metaData.Genres != null)
            {
                releaseGenres.AddRange(metaData.Genres);
            }
            var releaseLabels = new List<ReleaseLabelSearchResult>();
            var releaseMedias = new List<ReleaseMediaSearchResult>();
            var releaseImageUrls = new List<string>();

            var dontDoMetaDataProvidersSearchArtists = this.Configuration.DontDoMetaDataProvidersSearchArtists;
            if (!dontDoMetaDataProvidersSearchArtists.Any(x => x.Equals(metaData.Artist, StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    #region ITunes

                    if (this.ITunesReleaseSearchEngine.IsEnabled)
                    {
                        this.Logger.Trace("ITunesReleaseSearchEngine Release Search for ArtistName [{0}], ReleaseTitle [{1}]", metaData.Artist, result.Title);
                        var iTunesResult = await this.ITunesReleaseSearchEngine.PerformReleaseSearch(metaData.Artist, result.Title, 1);
                        if (iTunesResult.IsSuccess)
                        {
                            var i = iTunesResult.Data.First();
                            if (i.AlternateNames != null)
                            {
                                result.AlternateNames = result.AlternateNames.AddToDelimitedList(i.AlternateNames);
                            }
                            if (i.Tags != null)
                            {
                                result.Tags = result.Tags.AddToDelimitedList(i.Tags);
                            }
                            if (i.Urls != null)
                            {
                                result.URLs = result.URLs.AddToDelimitedList(i.Urls);
                            }
                            if (i.ImageUrls != null)
                            {
                                releaseImageUrls.AddRange(i.ImageUrls);
                            }
                            if (i.ReleaseGenres != null)
                            {
                                releaseGenres.AddRange(i.ReleaseGenres);
                            }
                            result.CopyTo(new Data.Release
                            {
                                ReleaseDate = result.ReleaseDate ?? i.ReleaseDate,
                                AmgId = i.AmgId,
                                Profile = i.Profile,
                                ITunesId = i.iTunesId,
                                Title = result.Title ?? i.ReleaseTitle,
                                Thumbnail = i.ReleaseThumbnailUrl != null ? WebHelper.BytesForImageUrl(i.ReleaseThumbnailUrl) : null,
                                ReleaseType = SafeParser.ToEnum<ReleaseType>(i.ReleaseType)
                            });
                            if (i.ReleaseLabel != null)
                            {
                                releaseLabels.AddRange(i.ReleaseLabel);
                            }
                            if (i.ReleaseMedia != null)
                            {
                                releaseMedias.AddRange(i.ReleaseMedia);
                            }
                        }
                        if (iTunesResult.Errors != null)
                        {
                            resultsExceptions.AddRange(iTunesResult.Errors);
                        }
                    }

                    #endregion ITunes

                    #region MusicBrainz

                    if (this.MusicBrainzReleaseSearchEngine.IsEnabled)
                    {
                        this.Logger.Trace("MusicBrainzReleaseSearchEngine Release Search for ArtistName [{0}], ReleaseTitle [{1}]", metaData.Artist, result.Title);
                        var mbResult = await this.MusicBrainzReleaseSearchEngine.PerformReleaseSearch(metaData.Artist, result.Title, 1);
                        if (mbResult.IsSuccess)
                        {
                            var mb = mbResult.Data.First();
                            if (mb.AlternateNames != null)
                            {
                                result.AlternateNames = result.AlternateNames.AddToDelimitedList(mb.AlternateNames);
                            }
                            if (mb.Tags != null)
                            {
                                result.Tags = result.Tags.AddToDelimitedList(mb.Tags);
                            }
                            if (mb.Urls != null)
                            {
                                result.URLs = result.URLs.AddToDelimitedList(mb.Urls);
                            }
                            if (mb.ImageUrls != null)
                            {
                                releaseImageUrls.AddRange(mb.ImageUrls);
                            }
                            if (mb.ReleaseGenres != null)
                            {
                                releaseGenres.AddRange(mb.ReleaseGenres);
                            }
                            if (!string.IsNullOrEmpty(mb.ReleaseTitle) && !mb.ReleaseTitle.Equals(result.Title, StringComparison.OrdinalIgnoreCase))
                            {
                                result.AlternateNames.AddToDelimitedList(new string[] { mb.ReleaseTitle });
                            }
                            result.CopyTo(new Data.Release
                            {
                                ReleaseDate = result.ReleaseDate ?? mb.ReleaseDate,
                                AmgId = mb.AmgId,
                                Profile = mb.Profile,
                                MusicBrainzId = mb.MusicBrainzId,
                                ITunesId = mb.iTunesId,
                                Title = result.Title ?? mb.ReleaseTitle,
                                Thumbnail = mb.ReleaseThumbnailUrl != null ? WebHelper.BytesForImageUrl(mb.ReleaseThumbnailUrl) : null,
                                ReleaseType = SafeParser.ToEnum<ReleaseType>(mb.ReleaseType)
                            });
                            if (mb.ReleaseLabel != null)
                            {
                                releaseLabels.AddRange(mb.ReleaseLabel);
                            }
                            if (mb.ReleaseMedia != null)
                            {
                                releaseMedias.AddRange(mb.ReleaseMedia);
                            }
                        }
                        if (mbResult.Errors != null)
                        {
                            resultsExceptions.AddRange(mbResult.Errors);
                        }
                    }

                    #endregion MusicBrainz

                    #region LastFm

                    if (this.LastFmReleaseSearchEngine.IsEnabled)
                    {
                        this.Logger.Trace("LastFmReleaseSearchEngine Release Search for ArtistName [{0}], ReleaseTitle [{1}]", metaData.Artist, result.Title);
                        var lastFmResult = await this.LastFmReleaseSearchEngine.PerformReleaseSearch(metaData.Artist, result.Title, 1);
                        if (lastFmResult.IsSuccess)
                        {
                            var l = lastFmResult.Data.First();
                            if (l.AlternateNames != null)
                            {
                                result.AlternateNames = result.AlternateNames.AddToDelimitedList(l.AlternateNames);
                            }
                            if (l.Tags != null)
                            {
                                result.Tags = result.Tags.AddToDelimitedList(l.Tags);
                            }
                            if (l.Urls != null)
                            {
                                result.URLs = result.URLs.AddToDelimitedList(l.Urls);
                            }
                            if (l.ImageUrls != null)
                            {
                                releaseImageUrls.AddRange(l.ImageUrls);
                            }
                            if (l.ReleaseGenres != null)
                            {
                                releaseGenres.AddRange(l.ReleaseGenres);
                            }
                            if (!string.IsNullOrEmpty(l.ReleaseTitle) && !l.ReleaseTitle.Equals(result.Title, StringComparison.OrdinalIgnoreCase))
                            {
                                result.AlternateNames.AddToDelimitedList(new string[] { l.ReleaseTitle });
                            }
                            result.CopyTo(new Data.Release
                            {
                                ReleaseDate = result.ReleaseDate ?? l.ReleaseDate,
                                AmgId = l.AmgId,
                                Profile = l.Profile,
                                LastFMId = l.LastFMId,
                                LastFMSummary = l.LastFMSummary,
                                MusicBrainzId = l.MusicBrainzId,
                                ITunesId = l.iTunesId,
                                Title = result.Title ?? l.ReleaseTitle,
                                Thumbnail = l.ReleaseThumbnailUrl != null ? WebHelper.BytesForImageUrl(l.ReleaseThumbnailUrl) : null,
                                ReleaseType = SafeParser.ToEnum<ReleaseType>(l.ReleaseType)
                            });
                            if (l.ReleaseLabel != null)
                            {
                                releaseLabels.AddRange(l.ReleaseLabel);
                            }
                            if (l.ReleaseMedia != null)
                            {
                                releaseMedias.AddRange(l.ReleaseMedia);
                            }
                        }
                        if (lastFmResult.Errors != null)
                        {
                            resultsExceptions.AddRange(lastFmResult.Errors);
                        }
                    }

                    #endregion LastFm

                    #region Spotify

                    if (this.SpotifyReleaseSearchEngine.IsEnabled)
                    {
                        this.Logger.Trace("SpotifyReleaseSearchEngine Release Search for ArtistName [{0}], ReleaseTitle [{1}]", metaData.Artist, result.Title);
                        var spotifyResult = await this.SpotifyReleaseSearchEngine.PerformReleaseSearch(metaData.Artist, result.Title, 1);
                        if (spotifyResult.IsSuccess)
                        {
                            var s = spotifyResult.Data.First();
                            if (s.Tags != null)
                            {
                                result.Tags = result.Tags.AddToDelimitedList(s.Tags);
                            }
                            if (s.Urls != null)
                            {
                                result.URLs = result.URLs.AddToDelimitedList(s.Urls);
                            }
                            if (s.ImageUrls != null)
                            {
                                releaseImageUrls.AddRange(s.ImageUrls);
                            }
                            if (s.ReleaseGenres != null)
                            {
                                releaseGenres.AddRange(s.ReleaseGenres);
                            }
                            if (!string.IsNullOrEmpty(s.ReleaseTitle) && !s.ReleaseTitle.Equals(result.Title, StringComparison.OrdinalIgnoreCase))
                            {
                                result.AlternateNames.AddToDelimitedList(new string[] { s.ReleaseTitle });
                            }
                            result.CopyTo(new Data.Release
                            {
                                ReleaseDate = result.ReleaseDate ?? s.ReleaseDate,
                                AmgId = s.AmgId,
                                Profile = this.HttpEncoder.HtmlEncode(s.Profile),
                                SpotifyId = s.SpotifyId,
                                MusicBrainzId = s.MusicBrainzId,
                                ITunesId = s.iTunesId,
                                Title = result.Title ?? s.ReleaseTitle,
                                Thumbnail = s.ReleaseThumbnailUrl != null ? WebHelper.BytesForImageUrl(s.ReleaseThumbnailUrl) : null,
                                ReleaseType = SafeParser.ToEnum<ReleaseType>(s.ReleaseType)
                            });
                            if (s.ReleaseLabel != null)
                            {
                                releaseLabels.AddRange(s.ReleaseLabel);
                            }
                            if (s.ReleaseMedia != null)
                            {
                                releaseMedias.AddRange(s.ReleaseMedia);
                            }
                        }
                        if (spotifyResult.Errors != null)
                        {
                            resultsExceptions.AddRange(spotifyResult.Errors);
                        }
                    }

                    #endregion Spotify

                    #region Discogs

                    if (this.DiscogsReleaseSearchEngine.IsEnabled)
                    {
                        this.Logger.Trace("DiscogsReleaseSearchEngine Release Search for ArtistName [{0}], ReleaseTitle [{1}]", metaData.Artist, result.Title);
                        var discogsResult = await this.DiscogsReleaseSearchEngine.PerformReleaseSearch(metaData.Artist, result.Title, 1);
                        if (discogsResult.IsSuccess)
                        {
                            var d = discogsResult.Data.First();
                            if (d.Urls != null)
                            {
                                result.URLs = result.URLs.AddToDelimitedList(d.Urls);
                            }
                            if (d.ImageUrls != null)
                            {
                                releaseImageUrls.AddRange(d.ImageUrls);
                            }
                            if (d.AlternateNames != null)
                            {
                                result.AlternateNames = result.AlternateNames.AddToDelimitedList(d.AlternateNames);
                            }
                            if (!string.IsNullOrEmpty(d.ReleaseTitle) && !d.ReleaseTitle.Equals(result.Title, StringComparison.OrdinalIgnoreCase))
                            {
                                result.AlternateNames.AddToDelimitedList(new string[] { d.ReleaseTitle });
                            }
                            result.CopyTo(new Data.Release
                            {
                                Profile = this.HttpEncoder.HtmlEncode(d.Profile),
                                DiscogsId = d.DiscogsId,
                                Title = result.Title ?? d.ReleaseTitle,
                                Thumbnail = d.ReleaseThumbnailUrl != null ? WebHelper.BytesForImageUrl(d.ReleaseThumbnailUrl) : null,
                                ReleaseType = SafeParser.ToEnum<ReleaseType>(d.ReleaseType)
                            });
                            if (d.ReleaseLabel != null)
                            {
                                releaseLabels.AddRange(d.ReleaseLabel);
                            }
                            if (d.ReleaseMedia != null)
                            {
                                releaseMedias.AddRange(d.ReleaseMedia);
                            }
                        }
                        if (discogsResult.Errors != null)
                        {
                            resultsExceptions.AddRange(discogsResult.Errors);
                        }
                    }

                    #endregion Discogs
                }
                catch (Exception ex)
                {
                    this._logger.Error(ex);
                }

                this.Logger.Trace("Metadata Providers Search Complete. [{0}]", sw.ElapsedMilliseconds);
            }
            else
            {
                this.Logger.Trace("Skipped Metadata Providers Search, DontDoMetaDataProvidersSearchArtists set for Artist [{0}].", metaData.Artist);
            }

            if (result.AlternateNames != null)
            {
                result.AlternateNames = string.Join("|", result.AlternateNames.ToListFromDelimited().Distinct().OrderBy(x => x));
            }
            if (result.URLs != null)
            {
                result.URLs = string.Join("|", result.URLs.ToListFromDelimited().Distinct().OrderBy(x => x));
            }
            if (result.Tags != null)
            {
                result.Tags = string.Join("|", result.Tags.ToListFromDelimited().Distinct().OrderBy(x => x));
            }
            if (releaseGenres.Any())
            {
                result.Genres = new List<ReleaseGenre>();
                foreach (var releaseGenre in releaseGenres.Where(x => !string.IsNullOrEmpty(x)).GroupBy(x => x).Select(x => x.First()))
                {
                    var rg = releaseGenre.Trim();
                    if (!string.IsNullOrEmpty(rg))
                    {
                        result.Genres.Add(new Data.ReleaseGenre
                        {
                            Genre = (this.DbContext.Genres.Where(x => x.Name.ToLower() == rg.ToLower()).FirstOrDefault() ?? new Data.Genre { Name = rg })
                        });
                    }
                };
            }
            if (releaseImageUrls.Any())
            {
                var imageBag = new ConcurrentBag<Data.Image>();
                var i = releaseImageUrls.Select(async url =>
                {
                    imageBag.Add(await WebHelper.GetImageFromUrlAsync(url));
                });
                await Task.WhenAll(i);
                // If the release has images merge any fetched images
                var existingImages = result.Images != null ? result.Images.ToList() : new List<Data.Image>();
                existingImages.AddRange(imageBag.ToList());
                // Now set release images to be unique image based on image hash
                result.Images = existingImages.Where(x => x != null && x.Bytes != null).GroupBy(x => x.Signature).Select(x => x.First()).Take(this.Configuration.Processing.MaximumReleaseImagesToAdd).ToList();
                if (result.Thumbnail == null && result.Images != null)
                {
                    result.Thumbnail = result.Images.First().Bytes;
                }
            }

            if (releaseLabels.Any())
            {
                result.Labels = releaseLabels.GroupBy(x => x.CatalogNumber).Select(x => x.First()).Select(x => new Data.ReleaseLabel
                {
                    CatalogNumber = x.CatalogNumber,
                    BeginDate = x.BeginDate,
                    EndDate = x.EndDate,
                    Status = Statuses.New,
                    Label = new Data.Label
                    {
                        Name = x.Label.LabelName,
                        SortName = x.Label.LabelSortName,
                        MusicBrainzId = x.Label.MusicBrainzId,
                        BeginDate = x.Label.StartDate,
                        EndDate = x.Label.EndDate,
                        ImageUrl = x.Label.LabelImageUrl,
                        AlternateNames = x.Label.AlternateNames.ToDelimitedList(),
                        URLs = x.Label.Urls.ToDelimitedList(),
                        Status = Statuses.New
                    }
                }).ToList();
            }

            if (releaseMedias.Any())
            {
                var resultReleaseMedias = new List<Data.ReleaseMedia>();
                foreach (var releaseMedia in releaseMedias.GroupBy(x => x.ReleaseMediaNumber).Select(x => x.First()))
                {
                    var rm = new Data.ReleaseMedia
                    {
                        MediaNumber = releaseMedia.ReleaseMediaNumber ?? 0,
                        SubTitle = releaseMedia.ReleaseMediaSubTitle,
                        TrackCount = releaseMedia.TrackCount ?? 0,
                        Status = Statuses.New
                    };
                    var rmTracks = new List<Data.Track>();
                    foreach (var releaseTrack in releaseMedias.Where(x => x.ReleaseMediaNumber == releaseMedia.ReleaseMediaNumber)
                                                             .SelectMany(x => x.Tracks)
                                                             .Where(x => x.TrackNumber.HasValue)
                                                             .OrderBy(x => x.TrackNumber))
                    {
                        var foundTrack = true;
                        var rmTrack = rmTracks.FirstOrDefault(x => x.TrackNumber == releaseTrack.TrackNumber.Value);
                        if (rmTrack == null)
                        {
                            Data.Artist trackArtist = null;
                            if (releaseTrack.Artist != null)
                            {
                                trackArtist = new Data.Artist
                                {
                                    Name = releaseTrack.Artist.ArtistName,
                                    SpotifyId = releaseTrack.Artist.SpotifyId,
                                    ArtistType = releaseTrack.Artist.ArtistType
                                };
                            }
                            rmTrack = new Data.Track
                            {
                                TrackArtist = trackArtist,
                                TrackArtists = releaseTrack.Artists,
                                TrackNumber = releaseTrack.TrackNumber.Value,
                                MusicBrainzId = releaseTrack.MusicBrainzId,
                                SpotifyId = releaseTrack.SpotifyId,
                                AmgId = releaseTrack.AmgId,
                                Title = releaseTrack.Title,
                                AlternateNames = releaseTrack.AlternateNames.ToDelimitedList(),
                                Duration = releaseTrack.Duration,
                                Tags = releaseTrack.Tags.ToDelimitedList(),
                                ISRC = releaseTrack.ISRC,
                                LastFMId = releaseTrack.LastFMId,
                                Status = Statuses.New
                            };
                            foundTrack = false;
                        }
                        rmTrack.Duration = rmTrack.Duration ?? releaseTrack.Duration;
                        rmTrack.MusicBrainzId = rmTrack.MusicBrainzId ?? releaseTrack.MusicBrainzId;
                        rmTrack.SpotifyId = rmTrack.SpotifyId ?? releaseTrack.SpotifyId;
                        rmTrack.AmgId = rmTrack.AmgId ?? releaseTrack.AmgId;
                        rmTrack.Title = rmTrack.Title ?? releaseTrack.Title;
                        rmTrack.Duration = releaseTrack.Duration;
                        rmTrack.Tags = rmTrack.Tags == null ? releaseTrack.Tags.ToDelimitedList() : rmTrack.Tags.AddToDelimitedList(releaseTrack.Tags);
                        rmTrack.AlternateNames = rmTrack.AlternateNames == null ? releaseTrack.AlternateNames.ToDelimitedList() : rmTrack.AlternateNames.AddToDelimitedList(releaseTrack.AlternateNames);
                        rmTrack.ISRC = rmTrack.ISRC ?? releaseTrack.ISRC;
                        rmTrack.LastFMId = rmTrack.LastFMId ?? releaseTrack.LastFMId;
                        if (!foundTrack)
                        {
                            rmTracks.Add(rmTrack);
                        }
                    }
                    rm.Tracks = rmTracks;
                    rm.TrackCount = (short)rmTracks.Count();
                    resultReleaseMedias.Add(rm);
                }
                result.Medias = resultReleaseMedias;
                result.TrackCount = (short)releaseMedias.SelectMany(x => x.Tracks).Count();
            }

            if (metaData.Images != null && metaData.Images.Any())
            {
                var image = metaData.Images.FirstOrDefault(x => x.Type == AudioMetaDataImageType.FrontCover);
                if (image == null)
                {
                    image = metaData.Images.FirstOrDefault();
                }
                // If there is an image on the metadata file itself then that over-rides metadata providers.
                if (image != null)
                {
                    result.Thumbnail = image.Data;
                }
            }
            if (!string.IsNullOrEmpty(artistFolder))
            {
                // If any file exist for cover that over-rides whatever if found in metadata providers.
                var releaseFolder = result.ReleaseFileFolder(artistFolder);
                if (Directory.Exists(releaseFolder))
                {
                    // See if there is a cover file ("cover.jpg") if so set thumbnail image to that
                    var coverFileName = Path.Combine(releaseFolder, ReleaseFactory.CoverFilename);
                    if (File.Exists(coverFileName))
                    {
                        // Read image and convert to jpeg
                        result.Thumbnail = File.ReadAllBytes(coverFileName);
                        this.Logger.Debug("Using Release Cover File [{0}]", coverFileName);
                    }
                }
            }

            if (result.Thumbnail != null)
            {
                result.Thumbnail = ImageHelper.ResizeImage(result.Thumbnail, this.Configuration.Thumbnails.Width, this.Configuration.Thumbnails.Height);
                result.Thumbnail = ImageHelper.ConvertToJpegFormat(result.Thumbnail);
            }
            sw.Stop();
            return new OperationResult<Data.Release>
            {
                Data = result,
                IsSuccess = result != null,
                Errors = resultsExceptions,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        /// <summary>
        /// For the given ReleaseId, Scan folder adding new, removing not found and updating DB tracks for tracks found
        /// </summary>
        public async Task<OperationResult<bool>> ScanReleaseFolder(Guid releaseId, string destinationFolder, bool doJustInfo, Data.Release releaseToScan = null)
        {
            SimpleContract.Requires<ArgumentOutOfRangeException>((releaseId != Guid.Empty && releaseToScan == null) || releaseToScan != null, "Invalid ReleaseId");

            var result = false;
            var resultErrors = new List<Exception>();
            var sw = new Stopwatch();
            sw.Start();
            var modifiedRelease = false;
            string releasePath = null;
            try
            {
                var release = releaseToScan ?? this.DbContext.Releases.Include(x => x.Artist).FirstOrDefault(x => x.RoadieId == releaseId);
                if (release == null)
                {
                    this.Logger.Fatal("Unable To Find Release [{0}]", releaseId);
                    return new OperationResult<bool>();
                }
                // This is recorded from metadata and if set then used to gauage if the release is complete
                short? totalTrackCount = null;
                short totalMissingCount = 0;
                releasePath = release.ReleaseFileFolder(release.Artist.ArtistFileFolder(this.Configuration, destinationFolder));
                var releaseDirectory = new DirectoryInfo(releasePath);
                if (!Directory.Exists(releasePath))
                {
                    this.Logger.Warning("Unable To Find Release Folder [{0}] For Release [{1}]", releasePath, release.ToString());
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
                            this.Logger.Warning("Track [{0}], File [{1}] Not Found.", existingTrack.ToString(), trackPath);
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

                var existingReleaseMedia = this.DbContext.ReleaseMedias.Include("tracks").Where(x => x.ReleaseId == release.Id).ToList();
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
                                        var trackArtistData = await this.ArtistFactory.GetByName(new AudioMetaData { Artist = audioMetaData.TrackArtist }, true);
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
                                        var trackArtistData = await this.ArtistFactory.GetByName(new AudioMetaData { Artist = audioMetaData.TrackArtist }, true);
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
                            this.Logger.Fatal("Release Track File Has Invalid MetaData [{0}]", audioMetaData.ToString());
                        }
                    }
                }
                else
                {
                    this.Logger.Warning("Unable To Find Releaes Path [{0}] For Release [{1}]", releasePath, release.ToString());
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
                            this.Logger.Debug("ReleaseMedia [{0}] Track Numbers Are Not Sequential", releaseMedia.Id);
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

                    await this.DbContext.SaveChangesAsync();
                    this.CacheManager.ClearRegion(release.Artist.CacheRegion);
                    this.CacheManager.ClearRegion(release.CacheRegion);
                }

                #endregion Scan Folder and Add or Update Existing Tracks from Files

                if (release.Thumbnail == null)
                {
                    // See if there is a cover file ("cover.jpg") if so set thumbnail image to that
                    var coverFileName = Path.Combine(releasePath, ReleaseFactory.CoverFilename);
                    if (File.Exists(coverFileName))
                    {
                        // Read image and convert to jpeg
                        release.Thumbnail = File.ReadAllBytes(coverFileName);
                        release.Thumbnail = ImageHelper.ResizeImage(release.Thumbnail, this.Configuration.Thumbnails.Width, this.Configuration.Thumbnails.Height);
                        release.Thumbnail = ImageHelper.ConvertToJpegFormat(release.Thumbnail);
                        release.LastUpdated = now;
                        await this.DbContext.SaveChangesAsync();
                        this.CacheManager.ClearRegion(release.Artist.CacheRegion);
                        this.CacheManager.ClearRegion(release.CacheRegion);
                        this.Logger.Info("Update Thumbnail using Release Cover File [{0}]", coverFileName);
                    }
                }

                sw.Stop();
                this.Logger.Info("Scanned Release [{0}] Folder [{1}], Modified Release [{2}], OperationTime [{3}]", release.ToString(), releasePath, modifiedRelease, sw.ElapsedMilliseconds);
                result = true;
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex, "ReleasePath [" + releasePath + "] " + ex.Serialize());
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

        public async Task<FactoryResult<Data.Release>> Update(Data.Release release, IEnumerable<Data.Image> releaseImages, string originalReleaseFolder, string destinationFolder = null)
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
                LabelId = x.Label.Id,
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
                        this.Logger.Error(ex);
                    }
                }
            }

            this._cacheManager.ClearRegion(release.CacheRegion);
            if (release.Artist != null)
            {
                this._cacheManager.ClearRegion(release.Artist.CacheRegion);
            }
            sw.Stop();

            return new FactoryResult<Data.Release>
            {
                Data = release,
                IsSuccess = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }
    }
}