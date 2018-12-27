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
using Roadie.Library.MetaData.ID3Tags;
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
        private List<int> _addedArtistIds = new List<int>();

        public IEnumerable<int> AddedArtistIds
        {
            get
            {
                return this._addedArtistIds;
            }
        }

        private IReleaseFactory ReleaseFactory { get; }
        private IImageFactory ImageFactory { get; }
        private IAudioMetaDataHelper AudioMetaDataHelper { get; }

        public ArtistFactory(IRoadieSettings configuration, IHttpEncoder httpEncoder, IRoadieDbContext context,
                             ICacheManager cacheManager, ILogger logger, IArtistLookupEngine artistLookupEngine, IReleaseFactory releaseFactory, IImageFactory imageFactory, IReleaseLookupEngine releaseLookupEngine, IAudioMetaDataHelper audioMetaDataHelper)
            : base(configuration, context, cacheManager, logger, httpEncoder, artistLookupEngine, releaseLookupEngine)
        {
            this.ReleaseFactory = releaseFactory;
            this.ImageFactory = imageFactory;
            this.AudioMetaDataHelper = audioMetaDataHelper;
        }

        public async Task<OperationResult<bool>> Delete(Guid RoadieId)
        {
            var isSuccess = false;
            var Artist = this.DbContext.Artists.FirstOrDefault(x => x.RoadieId == RoadieId);
            if (Artist != null)
            {
                return await this.Delete(Artist);
            }
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
                    this.DbContext.Artists.Remove(Artist);
                    await this.DbContext.SaveChangesAsync();
                    this.CacheManager.ClearRegion(Artist.CacheRegion);
                    this.Logger.LogInformation(string.Format("x DeleteArtist [{0}]", Artist.Id));
                    isSuccess = true;
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, ex.Serialize());
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

        public OperationResult<Artist> GetByExternalIds(string musicBrainzId = null, string iTunesId = null, string amgId = null, string spotifyId = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            var Artist = (from a in this.DbContext.Artists
                          where ((a.MusicBrainzId != null && (musicBrainzId != null && a.MusicBrainzId == musicBrainzId)) ||
                                 (a.ITunesId != null || (iTunesId != null && a.ITunesId == iTunesId)) ||
                                 (a.AmgId != null || (amgId != null && a.AmgId == amgId)) ||
                                 (a.SpotifyId != null || (spotifyId != null && a.SpotifyId == spotifyId)))
                          select a).FirstOrDefault();
            sw.Stop();
            if (Artist == null || !Artist.IsValid)
            {
                this.Logger.LogTrace("ArtistFactory: Artist Not Found By External Ids: MusicbrainzId [{0}], iTunesIs [{1}], AmgId [{2}], SpotifyId [{3}]", musicBrainzId, iTunesId, amgId, spotifyId);
            }
            return new OperationResult<Artist>
            {
                IsSuccess = Artist != null,
                OperationTime = sw.ElapsedMilliseconds,
                Data = Artist
            };
        }

        /// <summary>
        /// Merge one Artist into another one
        /// </summary>
        /// <param name="ArtistToMerge">The Artist to be merged</param>
        /// <param name="artistToMergeInto">The Artist to merge into</param>
        /// <returns></returns>
        public async Task<OperationResult<Artist>> MergeArtists(Artist ArtistToMerge, Artist artistToMergeInto, bool doDbUpdates = false)
        {
            SimpleContract.Requires<ArgumentNullException>(ArtistToMerge != null, "Invalid Artist");
            SimpleContract.Requires<ArgumentNullException>(artistToMergeInto != null, "Invalid Artist");

            var result = false;
            var now = DateTime.UtcNow;

            var sw = new Stopwatch();
            sw.Start();

            artistToMergeInto.RealName = ArtistToMerge.RealName ?? artistToMergeInto.RealName;
            artistToMergeInto.MusicBrainzId = ArtistToMerge.MusicBrainzId ?? artistToMergeInto.MusicBrainzId;
            artistToMergeInto.ITunesId = ArtistToMerge.ITunesId ?? artistToMergeInto.ITunesId;
            artistToMergeInto.AmgId = ArtistToMerge.AmgId ?? artistToMergeInto.AmgId;
            artistToMergeInto.SpotifyId = ArtistToMerge.SpotifyId ?? artistToMergeInto.SpotifyId;
            artistToMergeInto.Thumbnail = ArtistToMerge.Thumbnail ?? artistToMergeInto.Thumbnail;
            artistToMergeInto.Profile = ArtistToMerge.Profile ?? artistToMergeInto.Profile;
            artistToMergeInto.BirthDate = ArtistToMerge.BirthDate ?? artistToMergeInto.BirthDate;
            artistToMergeInto.BeginDate = ArtistToMerge.BeginDate ?? artistToMergeInto.BeginDate;
            artistToMergeInto.EndDate = ArtistToMerge.EndDate ?? artistToMergeInto.EndDate;
            if (!string.IsNullOrEmpty(ArtistToMerge.ArtistType) && !ArtistToMerge.ArtistType.Equals("Other", StringComparison.OrdinalIgnoreCase))
            {
                artistToMergeInto.ArtistType = ArtistToMerge.ArtistType;
            }
            artistToMergeInto.BioContext = ArtistToMerge.BioContext ?? artistToMergeInto.BioContext;
            artistToMergeInto.DiscogsId = ArtistToMerge.DiscogsId ?? artistToMergeInto.DiscogsId;

            artistToMergeInto.Tags = artistToMergeInto.Tags.AddToDelimitedList(ArtistToMerge.Tags.ToListFromDelimited());
            var altNames = ArtistToMerge.AlternateNames.ToListFromDelimited().ToList();
            altNames.Add(ArtistToMerge.Name);
            altNames.Add(ArtistToMerge.SortName);
            artistToMergeInto.AlternateNames = artistToMergeInto.AlternateNames.AddToDelimitedList(altNames);
            artistToMergeInto.URLs = artistToMergeInto.URLs.AddToDelimitedList(ArtistToMerge.URLs.ToListFromDelimited());
            artistToMergeInto.ISNI = artistToMergeInto.ISNI.AddToDelimitedList(ArtistToMerge.ISNI.ToListFromDelimited());
            artistToMergeInto.LastUpdated = now;

            if (doDbUpdates)
            {
                string sql = null;

                sql = "UPDATE `artistGenreTable` set artistId = " + artistToMergeInto.Id + " WHERE artistId = " + ArtistToMerge.Id + ";";
                await this.DbContext.Database.ExecuteSqlCommandAsync(sql);
                sql = "UPDATE `image` set artistId = " + artistToMergeInto.Id + " WHERE artistId = " + ArtistToMerge.Id + ";";
                await this.DbContext.Database.ExecuteSqlCommandAsync(sql);
                sql = "UPDATE `userArtist` set artistId = " + artistToMergeInto.Id + " WHERE artistId = " + ArtistToMerge.Id + ";";
                await this.DbContext.Database.ExecuteSqlCommandAsync(sql);
                sql = "UPDATE `track` set artistId = " + artistToMergeInto.Id + " WHERE artistId = " + ArtistToMerge.Id + ";";
                await this.DbContext.Database.ExecuteSqlCommandAsync(sql);

                try
                {
                    sql = "UPDATE `release` set artistId = " + artistToMergeInto.Id + " WHERE artistId = " + ArtistToMerge.Id + ";";
                    await this.DbContext.Database.ExecuteSqlCommandAsync(sql);
                }
                catch (Exception ex)
                {
                    this.Logger.LogWarning(ex.ToString());
                }
                var artistFolder = ArtistToMerge.ArtistFileFolder(this.Configuration, this.Configuration.LibraryFolder);
                foreach (var release in this.DbContext.Releases.Include("Artist").Where(x => x.ArtistId == ArtistToMerge.Id).ToArray())
                {
                    var originalReleaseFolder = release.ReleaseFileFolder(artistFolder);
                    await this.ReleaseFactory.Update(release, null, originalReleaseFolder);
                }

                await this.Delete(ArtistToMerge);
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
        /// Perform a Metadata Provider search and then merge the results into the given Artist
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
                var Artist = this.DbContext.Artists.FirstOrDefault(x => x.RoadieId == ArtistId);
                if (Artist == null)
                {
                    this.Logger.LogWarning("Unable To Find Artist [{0}]", ArtistId);
                    return new OperationResult<bool>();
                }

                OperationResult<Artist> ArtistSearch = null;
                try
                {
                    ArtistSearch = await this.ArtistLookupEngine.PerformMetaDataProvidersArtistSearch(new AudioMetaData
                    {
                        Artist = Artist.Name
                    });
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, ex.Serialize());
                }
                if (ArtistSearch.IsSuccess)
                {
                    // Do metadata search for Artist like if new Artist then set some overides and merge
                    var mergeResult = await this.MergeArtists(ArtistSearch.Data, Artist);
                    if (mergeResult.IsSuccess)
                    {
                        Artist = mergeResult.Data;
                        await this.DbContext.SaveChangesAsync();
                        sw.Stop();
                        this.CacheManager.ClearRegion(Artist.CacheRegion);
                        this.Logger.LogInformation("Scanned RefreshArtistMetadata [{0}], OperationTime [{1}]", Artist.ToString(), sw.ElapsedMilliseconds);
                    }
                    else
                    {
                        sw.Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, ex.Serialize());
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
        public async Task<OperationResult<bool>> ScanArtistReleasesFolders(Guid artistId, string destinationFolder, bool doJustInfo)
        {
            SimpleContract.Requires<ArgumentOutOfRangeException>(artistId != Guid.Empty, "Invalid ArtistId");

            var result = true;
            var resultErrors = new List<Exception>();
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                var artist = this.DbContext.Artists
                                           .Include("Releases")
                                           .Include("Releases.Labels")
                                           .FirstOrDefault(x => x.RoadieId == artistId);
                if (artist == null)
                {
                    this.Logger.LogWarning("Unable To Find Artist [{0}]", artistId);
                    return new OperationResult<bool>();
                }
                var releaseScannedCount = 0;
                var artistFolder = artist.ArtistFileFolder(this.Configuration, destinationFolder);
                var scannedArtistFolders = new List<string>();
                // Scan known releases for changes
                if (artist.Releases != null)
                {
                    foreach (var release in artist.Releases)
                    {
                        try
                        {
                            result = result && (await this.ReleaseFactory.ScanReleaseFolder(Guid.Empty, destinationFolder, doJustInfo, release)).Data;
                            releaseScannedCount++;
                            scannedArtistFolders.Add(release.ReleaseFileFolder(artistFolder));
                        }
                        catch (Exception ex)
                        {
                            this.Logger.LogError(ex, ex.Serialize());
                        }
                    }
                }
                // Any folder found in Artist folder not already scanned scan
                var folderProcessor = new FolderProcessor(this.Configuration, this.HttpEncoder, destinationFolder, this.DbContext, this.CacheManager, this.Logger, this.ArtistLookupEngine, this, this.ReleaseFactory, this.ImageFactory, this.ReleaseLookupEngine, this.AudioMetaDataHelper);
                var nonReleaseFolders = (from d in Directory.EnumerateDirectories(artistFolder)
                                         where !(from r in scannedArtistFolders select r).Contains(d)
                                         orderby d
                                         select d);
                foreach (var folder in nonReleaseFolders)
                {
                    await folderProcessor.Process(new DirectoryInfo(folder), doJustInfo);
                }
                if (!doJustInfo)
                {
                    FolderProcessor.DeleteEmptyFolders(new DirectoryInfo(artistFolder), this.Logger);
                }

                // Always update artist image if artist image is found on an artist rescan
                var imageFiles = ImageHelper.ImageFilesInFolder(artistFolder);
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
                        artist.Thumbnail = ImageHelper.ResizeImage(artist.Thumbnail, this.Configuration.MediumImageSize.Width, this.Configuration.MediumImageSize.Height);
                        artist.Thumbnail = ImageHelper.ConvertToJpegFormat(artist.Thumbnail);
                        artist.LastUpdated = DateTime.UtcNow;
                        await this.DbContext.SaveChangesAsync();
                        this.CacheManager.ClearRegion(artist.CacheRegion);
                        this.Logger.LogInformation("Update Thumbnail using Artist File [{0}]", iName);
                    }
                }

                sw.Stop();
                this.CacheManager.ClearRegion(artist.CacheRegion);
                this.Logger.LogInformation("Scanned Artist [{0}], Releases Scanned [{1}], OperationTime [{2}]", artist.ToString(), releaseScannedCount, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, ex.Serialize());
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
        public async Task<OperationResult<Artist>> Update(Artist Artist, IEnumerable<Image> ArtistImages, string destinationFolder = null)
        {
            SimpleContract.Requires<ArgumentNullException>(Artist != null, "Invalid Artist");

            var sw = new Stopwatch();
            sw.Start();

            var artistGenreTables = Artist.Genres.Select(x => new ArtistGenre { ArtistId = Artist.Id, GenreId = x.GenreId }).ToList();
            var artistAssociatedWith = Artist.AssociatedArtists.Select(x => new ArtistAssociation { ArtistId = Artist.Id, AssociatedArtistId = x.AssociatedArtistId }).ToList();
            var result = true;

            var now = DateTime.UtcNow;
            var originalArtistFolder = Artist.ArtistFileFolder(this.Configuration, destinationFolder ?? this.Configuration.LibraryFolder);
            var originalName = Artist.Name;
            var originalSortName = Artist.SortName;

            Artist.LastUpdated = now;
            await this.DbContext.SaveChangesAsync();

            this.DbContext.ArtistGenres.RemoveRange((from at in this.DbContext.ArtistGenres
                                                     where at.ArtistId == Artist.Id
                                                     select at));
            Artist.Genres = artistGenreTables;
            this.DbContext.ArtistAssociations.RemoveRange((from at in this.DbContext.ArtistAssociations
                                                           where at.ArtistId == Artist.Id
                                                           select at));
            Artist.AssociatedArtists = artistAssociatedWith;
            await this.DbContext.SaveChangesAsync();

            var existingImageIds = (from ai in ArtistImages
                                    where ai.Status != Statuses.New
                                    select ai.RoadieId).ToArray();
            this.DbContext.Images.RemoveRange((from i in this.DbContext.Images
                                               where i.ArtistId == Artist.Id
                                               where !(from x in existingImageIds select x).Contains(i.RoadieId)
                                               select i));
            await this.DbContext.SaveChangesAsync();
            if (ArtistImages != null && ArtistImages.Any(x => x.Status == Statuses.New))
            {
                foreach (var ArtistImage in ArtistImages.Where(x => x.Status == Statuses.New))
                {
                    this.DbContext.Images.Add(ArtistImage);
                }
                try
                {
                    await this.DbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, ex.Serialize());
                }
            }

            var newArtistFolder = Artist.ArtistFileFolder(this.Configuration, destinationFolder ?? this.Configuration.LibraryFolder);
            if (!originalArtistFolder.Equals(newArtistFolder, StringComparison.OrdinalIgnoreCase))
            {
                this.Logger.LogTrace("Moving Artist From Folder [{0}] To  [{1}]", originalArtistFolder, newArtistFolder);
                //  Directory.Move(originalArtistFolder, Artist.ArtistFileFolder(destinationFolder ?? SettingsHelper.Instance.LibraryFolder));
                // TODO if name changed then update Artist track files to have new Artist name
            }
            this.CacheManager.ClearRegion(Artist.CacheRegion);
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