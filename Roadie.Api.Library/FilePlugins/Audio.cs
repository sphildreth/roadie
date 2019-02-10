using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Extensions;
using Roadie.Library.Factories;
using Roadie.Library.Imaging;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Roadie.Library.FilePlugins
{
    public class Audio : PluginBase
    {
        private Guid _artistId = Guid.Empty;
        private Guid _releaseId = Guid.Empty;

        public IAudioMetaDataHelper AudioMetaDataHelper { get; }

        public override string[] HandlesTypes => new string[1] { "audio/mpeg" };

        public Audio(IRoadieSettings configuration,
            IHttpEncoder httpEncoder,
            IArtistFactory artistFactory,
            IReleaseFactory releaseFactory,
            IImageFactory imageFactory,
            ICacheManager cacheManager,
            ILogger logger,
            IArtistLookupEngine artistLookupEngine,
            IReleaseLookupEngine releaseLookupEngine,
            IAudioMetaDataHelper audioMetaDataHelper)
            : base(configuration, httpEncoder, artistFactory, releaseFactory, imageFactory, cacheManager, logger, artistLookupEngine, releaseLookupEngine)
        {
            this.AudioMetaDataHelper = audioMetaDataHelper;
        }

        public override async Task<OperationResult<bool>> Process(string destinationRoot, FileInfo fileInfo, bool doJustInfo, int? submissionId)
        {
            this.Logger.LogTrace(">> Audio: Process destinationRoot [{0}], FileInfo [{1}], doJustInfo [{2}], submissionId [{3}]", destinationRoot, fileInfo, doJustInfo, submissionId);
            var sw = Stopwatch.StartNew();
            var result = new OperationResult<bool>();

            try
            {
                var dr = destinationRoot ?? fileInfo.DirectoryName;

                string destinationName = null;

                var metaData = await this.AudioMetaDataHelper.GetInfo(fileInfo, doJustInfo);
                if (!metaData.IsValid)
                {
                    var minWeight = this.MinWeightToDelete;
                    if (metaData.ValidWeight < minWeight && minWeight > 0)
                    {
                        this.Logger.LogTrace("Invalid File{3}: ValidWeight [{0}], Under MinWeightToDelete [{1}]. Deleting File [{2}]", metaData.ValidWeight, minWeight, fileInfo.FullName, doJustInfo ? " [Read Only Mode] " : string.Empty);
                        if (!doJustInfo)
                        {
                            fileInfo.Delete();
                        }
                    }
                    return result;
                }

                var artist = metaData.Artist.CleanString(this.Configuration);
                var album = metaData.Release.CleanString(this.Configuration);
                var title = metaData.Title.CleanString(this.Configuration).ToTitleCase(false);
                var year = metaData.Year;
                var trackNumber = metaData.TrackNumber ?? 0;
                var diskNumber = metaData.Disk ?? 0;

                SimpleContract.Requires(metaData.IsValid, "Track MetaData Invalid");
                SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(artist), "Missing Track Artist");
                SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(album), "Missing Track Album");
                SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(title), "Missing Track Title");
                SimpleContract.Requires<ArgumentException>(year > 0, string.Format("Invalid Track Year [{0}]", year));
                SimpleContract.Requires<ArgumentException>(trackNumber > 0, "Missing Track Number");

                var artistFolder = await this.DetermineArtistFolder(dr, metaData, doJustInfo);
                if (string.IsNullOrEmpty(artistFolder))
                {
                    this.Logger.LogWarning("Unable To Find ArtistFolder [{0}] For MetaData [{1}]", artistFolder, metaData.ToString());
                    return new OperationResult<bool>("Unable To Find Artist Folder");
                }
                var releaseFolder = await this.DetermineReleaseFolder(artistFolder, metaData, doJustInfo, submissionId);
                if (string.IsNullOrEmpty(releaseFolder))
                {
                    this.Logger.LogWarning("Unable To Find ReleaseFolder For MetaData [{0}]", metaData.ToString());
                    return new OperationResult<bool>("Unable To Find Release Folder");
                }
                destinationName = FolderPathHelper.TrackFullPath(this.Configuration, metaData, dr, artistFolder, releaseFolder);
                this.Logger.LogTrace("Info: FileInfo [{0}], Artist Folder [{1}], Release Folder [{1}], Destination Name [{3}]", fileInfo.FullName, artistFolder, releaseFolder, destinationName);

                if (doJustInfo)
                {
                    result.IsSuccess = metaData.IsValid;
                    return result;
                }

                if(PluginBase.CheckMakeFolder(artistFolder))
                {
                    this.Logger.LogTrace("Created ArtistFolder [{0}]", artistFolder);
                }
                if(PluginBase.CheckMakeFolder(releaseFolder))
                {
                    this.Logger.LogTrace("Created ReleaseFolder [{0}]", releaseFolder);
                }

                try
                {
                    // See if file folder parent folder (likely file is in release folder) has primary artist image if so then move to artist folder
                    var artistImages = ImageHelper.FindImageTypeInDirectory(fileInfo.Directory, Enums.ImageType.Artist);
                    if (!artistImages.Any())
                    {
                        artistImages = ImageHelper.FindImageTypeInDirectory(fileInfo.Directory.Parent, Enums.ImageType.Artist);
                    }
                    if (artistImages.Any())
                    {
                        var artistImage = artistImages.First();
                        var aristImageFilename = Path.Combine(artistFolder, ImageHelper.ArtistImageFilename);
                        if (aristImageFilename != artistImage.FullName)
                        {
                            // Read image and convert to jpeg
                            var imageBytes = File.ReadAllBytes(artistImage.FullName);
                            imageBytes = ImageHelper.ConvertToJpegFormat(imageBytes);

                            // Move artist image to artist folder
                            if (!doJustInfo)
                            {
                                File.WriteAllBytes(aristImageFilename, imageBytes);
                                artistImage.Delete();
                            }
                            this.Logger.LogDebug("Found Artist Image File [{0}], Moved to artist folder.", artistImage.Name);
                        }
                    }

                    // See if any secondary artist images if so then move to artist folder
                    artistImages = ImageHelper.FindImageTypeInDirectory(fileInfo.Directory, Enums.ImageType.ArtistSecondary);
                    if (!artistImages.Any())
                    {
                        artistImages = ImageHelper.FindImageTypeInDirectory(fileInfo.Directory.Parent, Enums.ImageType.Artist);
                    }
                    if (artistImages.Any())
                    {
                        var looper = 0;
                        foreach (var artistImage in artistImages)
                        {
                            looper++;
                            var aristImageFilename = Path.Combine(artistFolder, string.Format(ImageHelper.ArtistSecondaryImageFilename, looper.ToString("00")));
                            if (aristImageFilename != artistImage.FullName)
                            {
                                // Read image and convert to jpeg
                                var imageBytes = File.ReadAllBytes(artistImage.FullName);
                                imageBytes = ImageHelper.ConvertToJpegFormat(imageBytes);

                                // Move artist image to artist folder
                                if (!doJustInfo)
                                {
                                    File.WriteAllBytes(aristImageFilename, imageBytes);
                                    artistImage.Delete();
                                }
                                this.Logger.LogDebug("Found Artist Secondary Image File [{0}], Moved to artist folder [{1}].", artistImage.Name, aristImageFilename);
                            }
                        }
                    }

                    // See if file folder has release image if so then move to release folder
                    var releaseImages = ImageHelper.FindImageTypeInDirectory(fileInfo.Directory, Enums.ImageType.Release);
                    if (releaseImages.Any())
                    {
                        var releaseImage = releaseImages.First();
                        var coverFileName = Path.Combine(releaseFolder, ImageHelper.ReleaseCoverFilename);
                        if (coverFileName != releaseImage.FullName)
                        {
                            // Read image and convert to jpeg
                            var imageBytes = File.ReadAllBytes(releaseImage.FullName);
                            imageBytes = ImageHelper.ConvertToJpegFormat(imageBytes);

                            // Move cover to release folder
                            if (!doJustInfo)
                            {
                                File.WriteAllBytes(coverFileName, imageBytes);
                                releaseImage.Delete();
                            }
                            this.Logger.LogDebug("Found Release Image File [{0}], Moved to release folder", releaseImage.Name);
                        }
                    }

                    // See if folder has secondary release image if so then move to release folder
                    releaseImages = ImageHelper.FindImageTypeInDirectory(fileInfo.Directory, Enums.ImageType.ReleaseSecondary);
                    if (releaseImages.Any())
                    {
                        var looper = 0;
                        foreach (var releaseImage in releaseImages)
                        {
                            looper++;
                            var releaseImageFilename = Path.Combine(releaseFolder, string.Format(ImageHelper.ReleaseSecondaryImageFilename, looper.ToString("00")));
                            if (releaseImageFilename != releaseImage.FullName)
                            {
                                // Read image and convert to jpeg
                                var imageBytes = File.ReadAllBytes(releaseImage.FullName);
                                imageBytes = ImageHelper.ConvertToJpegFormat(imageBytes);

                                // Move cover to release folder
                                if (!doJustInfo)
                                {
                                    File.WriteAllBytes(releaseImageFilename, imageBytes);
                                    releaseImage.Delete();
                                }
                                this.Logger.LogDebug("Found Release Image File [{0}], Moved to release folder [{1}]", releaseImage.Name, releaseImageFilename);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "Error with Managing Images For [{0}]", fileInfo.FullName);
                }

                var doesFileExistsForTrack = File.Exists(destinationName);
                if (doesFileExistsForTrack)
                {
                    var existing = new FileInfo(destinationName);

                    // If Exists determine which is better - if same do nothing
                    var existingMetaData = await this.AudioMetaDataHelper.GetInfo(existing, doJustInfo);

                    var areSameFile = existing.FullName.Replace("\\", "").Replace("/", "").Equals(fileInfo.FullName.Replace("\\", "").Replace("/", ""), StringComparison.OrdinalIgnoreCase);
                    var currentBitRate = metaData.AudioBitrate;
                    var existingBitRate = existingMetaData.AudioBitrate;

                    if (!areSameFile)
                    {
                        if (!existingMetaData.IsValid || (currentBitRate > existingBitRate))
                        {
                            this.Logger.LogTrace("Newer Is Better: Deleting Existing File [{0}]", existing);
                            if (!doJustInfo)
                            {
                                existing.Delete();
                                fileInfo.MoveTo(destinationName);
                            }
                        }
                        else
                        {
                            this.Logger.LogTrace("Existing [{0}] Is Better or Equal: Deleting Found File [{1}]", existing, fileInfo.FullName);
                            if (!doJustInfo)
                            {
                                fileInfo.Delete();
                            }
                        }
                    }
                }
                else
                {
                    this.Logger.LogTrace("Moving File To [{0}]", destinationName);
                    if (!doJustInfo)
                    {
                        try
                        {
                            fileInfo.MoveTo(destinationName);
                        }
                        catch (Exception ex)
                        {
                            this.Logger.LogError(ex, "Error Moving File [{0}}", destinationName);
                        }
                    }
                }

                result.AdditionalData.Add(PluginResultInfo.AdditionalDataKeyPluginResultInfo, new PluginResultInfo
                {
                    ArtistFolder = artistFolder,
                    ArtistId = this._artistId,
                    ReleaseFolder = releaseFolder,
                    ReleaseId = this._releaseId,
                    Filename = fileInfo.FullName,
                    TrackNumber = metaData.TrackNumber,
                    TrackTitle = metaData.Title
                });
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Error Processing File [{0}}", fileInfo);
            }

            sw.Stop();
            this.Logger.LogTrace("<< Audio: Process Complete. Result `{0}`, ElapsedTime [{1}]", JsonConvert.SerializeObject(result), sw.ElapsedMilliseconds);
            return result;
        }

        private async Task<string> DetermineArtistFolder(string destinationRoot, AudioMetaData metaData, bool doJustInfo)
        {
            var artist = await this.ArtistLookupEngine.GetByName(metaData, !doJustInfo);
            if (!artist.IsSuccess)
            {
                return null;
            }
            try
            {
                return artist.Data.ArtistFileFolder(this.Configuration, destinationRoot);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, ex.Serialize());
            }
            return null;
        }

        private async Task<string> DetermineReleaseFolder(string artistFolder, AudioMetaData metaData, bool doJustInfo, int? submissionId)
        {
            var artist = await this.ArtistLookupEngine.GetByName(metaData, !doJustInfo);
            if (!artist.IsSuccess)
            {
                return null;
            }
            this._artistId = artist.Data.RoadieId;
            var release = await this.ReleaseLookupEngine.GetByName(artist.Data, metaData, !doJustInfo, submissionId: submissionId);
            if (!release.IsSuccess)
            {
                return null;
            }
            this._releaseId = release.Data.RoadieId;
            release.Data.ReleaseDate = SafeParser.ToDateTime(metaData.Year);
            return release.Data.ReleaseFileFolder(artistFolder);
        }
    }
}