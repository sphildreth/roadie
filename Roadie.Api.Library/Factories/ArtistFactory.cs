using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.Factories
{
#pragma warning disable EF1000

    public sealed class ArtistFactory : FactoryBase, IArtistFactory
    {
        private readonly List<int> _addedArtistIds = new List<int>();

        public ArtistFactory(IRoadieSettings configuration, IHttpEncoder httpEncoder, IRoadieDbContext context,
            ICacheManager cacheManager, ILogger logger, IArtistLookupEngine artistLookupEngine,
            IReleaseFactory releaseFactory, IImageFactory imageFactory, IReleaseLookupEngine releaseLookupEngine,
            IAudioMetaDataHelper audioMetaDataHelper)
            : base(configuration, context, cacheManager, logger, httpEncoder, artistLookupEngine, releaseLookupEngine)
        {
            ReleaseFactory = releaseFactory;
            ImageFactory = imageFactory;
            AudioMetaDataHelper = audioMetaDataHelper;
        }

        public IEnumerable<int> AddedArtistIds => _addedArtistIds;

        private IReleaseFactory ReleaseFactory { get; }
        private IImageFactory ImageFactory { get; }
        private IAudioMetaDataHelper AudioMetaDataHelper { get; }

        public async Task<OperationResult<bool>> Delete(Guid RoadieId)
        {
            var isSuccess = false;
            var Artist = DbContext.Artists.FirstOrDefault(x => x.RoadieId == RoadieId);
            if (Artist != null) return await Delete(Artist);
            return new OperationResult<bool>
            {
                Data = isSuccess
            };
        }

        public async Task<OperationResult<bool>> Delete(Artist Artist)
        {
            var isSuccess = false;
            try
            {
                if (Artist != null)
                {
                    DbContext.Artists.Remove(Artist);
                    await DbContext.SaveChangesAsync();
                    CacheManager.ClearRegion(Artist.CacheRegion);
                    Logger.LogInformation(string.Format("x DeleteArtist [{0}]", Artist.Id));
                    isSuccess = true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Serialize());
                return new OperationResult<bool>
                {
                    Errors = new Exception[1] { ex }
                };
            }

            return new OperationResult<bool>
            {
                IsSuccess = isSuccess,
                Data = isSuccess
            };
        }

        public OperationResult<Artist> GetByExternalIds(string musicBrainzId = null, string iTunesId = null,
            string amgId = null, string spotifyId = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            var Artist = (from a in DbContext.Artists
                          where a.MusicBrainzId != null && musicBrainzId != null && a.MusicBrainzId == musicBrainzId ||
                                a.ITunesId != null || iTunesId != null && a.ITunesId == iTunesId || a.AmgId != null ||
                                amgId != null && a.AmgId == amgId || a.SpotifyId != null ||
                                spotifyId != null && a.SpotifyId == spotifyId
                          select a).FirstOrDefault();
            sw.Stop();
            if (Artist == null || !Artist.IsValid)
                Logger.LogTrace(
                    "ArtistFactory: Artist Not Found By External Ids: MusicbrainzId [{0}], iTunesIs [{1}], AmgId [{2}], SpotifyId [{3}]",
                    musicBrainzId, iTunesId, amgId, spotifyId);
            return new OperationResult<Artist>
            {
                IsSuccess = Artist != null,
                OperationTime = sw.ElapsedMilliseconds,
                Data = Artist
            };
        }

        /// <summary>
        ///     Merge one Artist into another one
        /// </summary>
        /// <param name="artistToMerge">The Artist to be merged</param>
        /// <param name="artistToMergeInto">The Artist to merge into</param>
        /// <returns></returns>
        public async Task<OperationResult<Artist>> MergeArtists(Artist artistToMerge, Artist artistToMergeInto,
            bool doDbUpdates = false)
        {
            SimpleContract.Requires<ArgumentNullException>(artistToMerge != null, "Invalid Artist");
            SimpleContract.Requires<ArgumentNullException>(artistToMergeInto != null, "Invalid Artist");

            var result = false;
            var now = DateTime.UtcNow;

            var sw = new Stopwatch();
            sw.Start();

            artistToMergeInto.RealName = artistToMerge.RealName ?? artistToMergeInto.RealName;
            artistToMergeInto.MusicBrainzId = artistToMerge.MusicBrainzId ?? artistToMergeInto.MusicBrainzId;
            artistToMergeInto.ITunesId = artistToMerge.ITunesId ?? artistToMergeInto.ITunesId;
            artistToMergeInto.AmgId = artistToMerge.AmgId ?? artistToMergeInto.AmgId;
            artistToMergeInto.SpotifyId = artistToMerge.SpotifyId ?? artistToMergeInto.SpotifyId;
            artistToMergeInto.Thumbnail = artistToMerge.Thumbnail ?? artistToMergeInto.Thumbnail;
            artistToMergeInto.Profile = artistToMerge.Profile ?? artistToMergeInto.Profile;
            artistToMergeInto.BirthDate = artistToMerge.BirthDate ?? artistToMergeInto.BirthDate;
            artistToMergeInto.BeginDate = artistToMerge.BeginDate ?? artistToMergeInto.BeginDate;
            artistToMergeInto.EndDate = artistToMerge.EndDate ?? artistToMergeInto.EndDate;
            if (!string.IsNullOrEmpty(artistToMerge.ArtistType) && !artistToMerge.ArtistType.Equals("Other", StringComparison.OrdinalIgnoreCase))
            {
                artistToMergeInto.ArtistType = artistToMerge.ArtistType;
            }
            artistToMergeInto.BioContext = artistToMerge.BioContext ?? artistToMergeInto.BioContext;
            artistToMergeInto.DiscogsId = artistToMerge.DiscogsId ?? artistToMergeInto.DiscogsId;
            artistToMergeInto.Tags = artistToMergeInto.Tags.AddToDelimitedList(artistToMerge.Tags.ToListFromDelimited());
            var altNames = artistToMerge.AlternateNames.ToListFromDelimited().ToList();
            altNames.Add(artistToMerge.Name);
            altNames.Add(artistToMerge.SortName);
            artistToMergeInto.AlternateNames = artistToMergeInto.AlternateNames.AddToDelimitedList(altNames);
            artistToMergeInto.URLs = artistToMergeInto.URLs.AddToDelimitedList(artistToMerge.URLs.ToListFromDelimited());
            artistToMergeInto.ISNI = artistToMergeInto.ISNI.AddToDelimitedList(artistToMerge.ISNI.ToListFromDelimited());
            artistToMergeInto.LastUpdated = now;

            if (doDbUpdates)
            {
                try
                {
                    var artistGenres = DbContext.ArtistGenres.Where(x => x.ArtistId == artistToMerge.Id).ToArray();
                    if (artistGenres != null)
                    {
                        foreach (var artistGenre in artistGenres)
                        {
                            artistGenre.ArtistId = artistToMergeInto.Id;
                        }
                    }
                    var artistImages = DbContext.Images.Where(x => x.ArtistId == artistToMerge.Id).ToArray();
                    if (artistImages != null)
                    {
                        foreach (var artistImage in artistImages)
                        {
                            artistImage.ArtistId = artistToMergeInto.Id;
                        }
                    }
                    var userArtists = DbContext.UserArtists.Where(x => x.ArtistId == artistToMerge.Id).ToArray();
                    if (artistImages != null)
                    {
                        foreach (var userArtist in userArtists)
                        {
                            userArtist.ArtistId = artistToMergeInto.Id;
                        }
                    }
                    var artistTracks = DbContext.Tracks.Where(x => x.ArtistId == artistToMerge.Id).ToArray();
                    if (artistTracks != null)
                    {
                        foreach (var artistTrack in artistTracks)
                        {
                            artistTrack.ArtistId = artistToMergeInto.Id;
                        }
                    }
                    var artistReleases = DbContext.Releases.Where(x => x.ArtistId == artistToMerge.Id).ToArray();
                    if (artistReleases != null)
                    {
                        foreach (var artistRelease in artistReleases)
                        {
                            // See if there is already a release by the same name for the artist to merge into, if so then merge releases
                            var artistToMergeHasRelease = DbContext.Releases.FirstOrDefault(x => x.ArtistId == artistToMerge.Id && x.Title == artistRelease.Title);
                            if (artistToMergeHasRelease != null)
                            {
                                await ReleaseFactory.MergeReleases(artistRelease, artistToMergeHasRelease, false);
                            }
                            else
                            {
                                artistRelease.ArtistId = artistToMerge.Id;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex.ToString());
                }

                var artistFolder = artistToMerge.ArtistFileFolder(Configuration, Configuration.LibraryFolder);
                foreach (var release in DbContext.Releases.Include("Artist").Where(x => x.ArtistId == artistToMerge.Id).ToArray())
                {
                    var originalReleaseFolder = release.ReleaseFileFolder(artistFolder);
                    await ReleaseFactory.Update(release, null, originalReleaseFolder);
                }
                await Delete(artistToMerge);
            }

            result = true;

            sw.Stop();
            return new OperationResult<Artist>
            {
                Data = artistToMergeInto,
                IsSuccess = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }

        /// <summary>
        ///     Perform a Metadata Provider search and then merge the results into the given Artist
        /// </summary>
        /// <param name="ArtistId">Given Artist RoadieId</param>
        /// <returns>Operation Result</returns>
        public async Task<OperationResult<bool>> RefreshArtistMetadata(Guid ArtistId)
        {
            SimpleContract.Requires<ArgumentOutOfRangeException>(ArtistId != Guid.Empty, "Invalid ArtistId");

            var result = true;
            var resultErrors = new List<Exception>();
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                var Artist = DbContext.Artists.FirstOrDefault(x => x.RoadieId == ArtistId);
                if (Artist == null)
                {
                    Logger.LogWarning("Unable To Find Artist [{0}]", ArtistId);
                    return new OperationResult<bool>();
                }

                OperationResult<Artist> ArtistSearch = null;
                try
                {
                    ArtistSearch = await ArtistLookupEngine.PerformMetaDataProvidersArtistSearch(new AudioMetaData
                    {
                        Artist = Artist.Name
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Serialize());
                }

                if (ArtistSearch.IsSuccess)
                {
                    // Do metadata search for Artist like if new Artist then set some overides and merge
                    var mergeResult = await MergeArtists(ArtistSearch.Data, Artist);
                    if (mergeResult.IsSuccess)
                    {
                        Artist = mergeResult.Data;
                        await DbContext.SaveChangesAsync();
                        sw.Stop();
                        CacheManager.ClearRegion(Artist.CacheRegion);
                        Logger.LogInformation("Scanned RefreshArtistMetadata [{0}], OperationTime [{1}]",
                            Artist.ToString(), sw.ElapsedMilliseconds);
                    }
                    else
                    {
                        sw.Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Serialize());
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
        public async Task<OperationResult<bool>> ScanArtistReleasesFolders(Guid artistId, string destinationFolder,
            bool doJustInfo)
        {
            SimpleContract.Requires<ArgumentOutOfRangeException>(artistId != Guid.Empty, "Invalid ArtistId");

            var result = true;
            var resultErrors = new List<Exception>();
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                var artist = DbContext.Artists
                    .Include("Releases")
                    .Include("Releases.Labels")
                    .FirstOrDefault(x => x.RoadieId == artistId);
                if (artist == null)
                {
                    Logger.LogWarning("Unable To Find Artist [{0}]", artistId);
                    return new OperationResult<bool>();
                }

                var releaseScannedCount = 0;
                var artistFolder = artist.ArtistFileFolder(Configuration, destinationFolder);
                var scannedArtistFolders = new List<string>();
                // Scan known releases for changes
                if (artist.Releases != null)
                    foreach (var release in artist.Releases)
                        try
                        {
                            result = result && (await ReleaseFactory.ScanReleaseFolder(Guid.Empty, destinationFolder,
                                         doJustInfo, release)).Data;
                            releaseScannedCount++;
                            scannedArtistFolders.Add(release.ReleaseFileFolder(artistFolder));
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, ex.Serialize());
                        }

                // Any folder found in Artist folder not already scanned scan
                var folderProcessor = new FolderProcessor(Configuration, HttpEncoder, destinationFolder, DbContext,
                    CacheManager, Logger, ArtistLookupEngine, this, ReleaseFactory, ImageFactory, ReleaseLookupEngine,
                    AudioMetaDataHelper);
                var nonReleaseFolders = from d in Directory.EnumerateDirectories(artistFolder)
                                        where !(from r in scannedArtistFolders select r).Contains(d)
                                        orderby d
                                        select d;
                foreach (var folder in nonReleaseFolders)
                    await folderProcessor.Process(new DirectoryInfo(folder), doJustInfo);
                if (!doJustInfo) FolderProcessor.DeleteEmptyFolders(new DirectoryInfo(artistFolder), Logger);

                // Always update artist image if artist image is found on an artist rescan
                var imageFiles = ImageHelper.ImageFilesInFolder(artistFolder, SearchOption.AllDirectories);
                if (imageFiles != null && imageFiles.Any())
                {
                    var imageFile = imageFiles.First();
                    var i = new FileInfo(imageFile);
                    var iName = i.Name.ToLower().Trim();
                    var isArtistImage = iName.Contains("artist") || iName.Contains(artist.Name.ToLower());
                    if (isArtistImage)
                    {
                        // Read image and convert to jpeg
                        artist.Thumbnail = File.ReadAllBytes(i.FullName);
                        artist.Thumbnail = ImageHelper.ResizeImage(artist.Thumbnail,
                            Configuration.MediumImageSize.Width, Configuration.MediumImageSize.Height);
                        artist.Thumbnail = ImageHelper.ConvertToJpegFormat(artist.Thumbnail);
                        if (artist.Thumbnail.Length >= ImageHelper.MaximumThumbnailByteSize)
                        {
                            Logger.LogWarning(
                                $"Artist Thumbnail larger than maximum size after resizing to [{Configuration.ThumbnailImageSize.Width}x{Configuration.ThumbnailImageSize.Height}] Thumbnail Size [{artist.Thumbnail.Length}]");
                            artist.Thumbnail = null;
                        }

                        artist.LastUpdated = DateTime.UtcNow;
                        await DbContext.SaveChangesAsync();
                        CacheManager.ClearRegion(artist.CacheRegion);
                        Logger.LogInformation("Update Thumbnail using Artist File [{0}]", iName);
                    }
                }

                sw.Stop();
                CacheManager.ClearRegion(artist.CacheRegion);
                Logger.LogInformation("Scanned Artist [{0}], Releases Scanned [{1}], OperationTime [{2}]",
                    artist.ToString(), releaseScannedCount, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Serialize());
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
        public async Task<OperationResult<Artist>> Update(Artist Artist, IEnumerable<Image> ArtistImages,
            string destinationFolder = null)
        {
            SimpleContract.Requires<ArgumentNullException>(Artist != null, "Invalid Artist");

            var sw = new Stopwatch();
            sw.Start();

            var artistGenreTables = Artist.Genres
                .Select(x => new ArtistGenre { ArtistId = Artist.Id, GenreId = x.GenreId }).ToList();
            var artistAssociatedWith = Artist.AssociatedArtists.Select(x => new ArtistAssociation
            { ArtistId = Artist.Id, AssociatedArtistId = x.AssociatedArtistId }).ToList();
            var similarArtists = Artist.SimilarArtists.Select(x => new ArtistSimilar
            { ArtistId = Artist.Id, SimilarArtistId = x.SimilarArtistId }).ToList();
            var result = true;

            var now = DateTime.UtcNow;
            var originalArtistFolder =
                Artist.ArtistFileFolder(Configuration, destinationFolder ?? Configuration.LibraryFolder);
            var originalName = Artist.Name;
            var originalSortName = Artist.SortName;

            Artist.LastUpdated = now;
            await DbContext.SaveChangesAsync();

            DbContext.ArtistGenres.RemoveRange(from at in DbContext.ArtistGenres
                                               where at.ArtistId == Artist.Id
                                               select at);
            Artist.Genres = artistGenreTables;
            DbContext.ArtistAssociations.RemoveRange(from at in DbContext.ArtistAssociations
                                                     where at.ArtistId == Artist.Id
                                                     select at);
            Artist.AssociatedArtists = artistAssociatedWith;
            Artist.SimilarArtists = similarArtists;
            await DbContext.SaveChangesAsync();

            var existingImageIds = (from ai in ArtistImages
                                    where ai.Status != Statuses.New
                                    select ai.RoadieId).ToArray();
            DbContext.Images.RemoveRange(from i in DbContext.Images
                                         where i.ArtistId == Artist.Id
                                         where !(from x in existingImageIds select x).Contains(i.RoadieId)
                                         select i);
            await DbContext.SaveChangesAsync();
            if (ArtistImages != null && ArtistImages.Any(x => x.Status == Statuses.New))
            {
                foreach (var ArtistImage in ArtistImages.Where(x => x.Status == Statuses.New))
                    DbContext.Images.Add(ArtistImage);
                try
                {
                    await DbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Serialize());
                }
            }

            var newArtistFolder =
                Artist.ArtistFileFolder(Configuration, destinationFolder ?? Configuration.LibraryFolder);
            if (!originalArtistFolder.Equals(newArtistFolder, StringComparison.OrdinalIgnoreCase))
                Logger.LogTrace("Moving Artist From Folder [{0}] To  [{1}]", originalArtistFolder, newArtistFolder);
            //  Directory.Move(originalArtistFolder, Artist.ArtistFileFolder(destinationFolder ?? SettingsHelper.Instance.LibraryFolder));
            // TODO if name changed then update Artist track files to have new Artist name
            CacheManager.ClearRegion(Artist.CacheRegion);
            sw.Stop();

            return new OperationResult<Artist>
            {
                Data = Artist,
                IsSuccess = result,
                OperationTime = sw.ElapsedMilliseconds
            };
        }
    }
}