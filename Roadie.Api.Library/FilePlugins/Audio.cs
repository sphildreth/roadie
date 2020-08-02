using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Enums;
using Roadie.Library.Extensions;
using Roadie.Library.Imaging;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Roadie.Library.FilePlugins
{
    public class Audio : PluginBase
    {
        private Guid _artistId = Guid.Empty;
        private Guid _releaseId = Guid.Empty;

        public IAudioMetaDataHelper AudioMetaDataHelper { get; }

        public override string[] HandlesTypes => new string[1] { MimeTypeHelper.Mp3MimeType };

        public Audio(IRoadieSettings configuration, IHttpEncoder httpEncoder, ICacheManager cacheManager,
                     ILogger logger, IArtistLookupEngine artistLookupEngine, IReleaseLookupEngine releaseLookupEngine,
                     IAudioMetaDataHelper audioMetaDataHelper)
            : base(configuration, httpEncoder, cacheManager, logger, artistLookupEngine, releaseLookupEngine)
        {
            AudioMetaDataHelper = audioMetaDataHelper;
        }

        public override async Task<OperationResult<bool>> Process(FileInfo fileInfo, bool doJustInfo, int? submissionId)
        {
            Logger.LogTrace($">> Audio: Process FileInfo [{fileInfo}], doJustInfo [{doJustInfo}], submissionId [{submissionId}]", fileInfo, doJustInfo, submissionId);
            var sw = Stopwatch.StartNew();
            var result = new OperationResult<bool>();

            try
            {
                string destinationName = null;

                var metaData = await AudioMetaDataHelper.GetInfo(fileInfo, doJustInfo).ConfigureAwait(false);
                if (!metaData.IsValid)
                {
                    var minWeight = MinWeightToDelete;
                    if (metaData.ValidWeight < minWeight && minWeight > 0)
                    {
                        Logger.LogTrace(
                            "Invalid File{3}: ValidWeight [{0}], Under MinWeightToDelete [{1}]. Deleting File [{2}]",
                            metaData.ValidWeight, minWeight, fileInfo.FullName,
                            doJustInfo ? " [Read Only Mode] " : string.Empty);
                        if (!doJustInfo) fileInfo.Delete();
                    }

                    return result;
                }

                var artist = metaData.Artist.CleanString(Configuration);
                var album = metaData.Release.CleanString(Configuration);
                var title = metaData.Title.CleanString(Configuration).ToTitleCase(false);
                var year = metaData.Year;
                var trackNumber = metaData.TrackNumber ?? 0;
                var discNumber = metaData.Disc ?? 0;

                SimpleContract.Requires(metaData.IsValid, "Track MetaData Invalid");
                SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(artist), "Missing Track Artist");
                SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(album), "Missing Track Album");
                SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(title), "Missing Track Title");
                SimpleContract.Requires<ArgumentException>(year > 0, string.Format("Invalid Track Year [{0}]", year));
                SimpleContract.Requires<ArgumentException>(trackNumber > 0, "Missing Track Number");

                var artistFolder = await DetermineArtistFolder(metaData, doJustInfo).ConfigureAwait(false);
                if (string.IsNullOrEmpty(artistFolder))
                {
                    Logger.LogWarning("Unable To Find ArtistFolder [{0}] For MetaData [{1}]", artistFolder,
                        metaData.ToString());
                    return new OperationResult<bool>("Unable To Find Artist Folder");
                }

                var releaseFolder = await DetermineReleaseFolder(artistFolder, metaData, doJustInfo, submissionId).ConfigureAwait(false);
                if (string.IsNullOrEmpty(releaseFolder))
                {
                    Logger.LogWarning("Unable To Find ReleaseFolder For MetaData [{0}]", metaData.ToString());
                    return new OperationResult<bool>("Unable To Find Release Folder");
                }

                destinationName = FolderPathHelper.TrackFullPath(Configuration, metaData, artistFolder, releaseFolder);
                Logger.LogTrace("Info: FileInfo [{0}], Artist Folder [{1}], Release Folder [{1}], Destination Name [{3}]", fileInfo.FullName, artistFolder, releaseFolder, destinationName);
                if (doJustInfo)
                {
                    result.IsSuccess = metaData.IsValid;
                    return result;
                }

                if (CheckMakeFolder(artistFolder))
                {
                    Logger.LogTrace("Created ArtistFolder [{0}]", artistFolder);
                }
                if (CheckMakeFolder(releaseFolder))
                {
                    Logger.LogTrace("Created ReleaseFolder [{0}]", releaseFolder);
                }

                string imageFilename = null;
                try
                {
                    // See if file folder parent folder (likely file is in release folder) has primary artist image if so then move to artist folder
                    var artistImages = new List<FileInfo>();
                    artistImages.AddRange(ImageHelper.FindImageTypeInDirectory(fileInfo.Directory, ImageType.Artist));
                    artistImages.AddRange(ImageHelper.FindImageTypeInDirectory(fileInfo.Directory.Parent, ImageType.Artist));
                    if (artistImages.Count > 0)
                    {
                        var artistImage = artistImages[0];
                        imageFilename = Path.Combine(artistFolder, ImageHelper.ArtistImageFilename);
                        if (imageFilename != artistImage.FullName)
                        {
                            // Read image and convert to jpeg
                            var imageBytes = File.ReadAllBytes(artistImage.FullName);
                            imageBytes = ImageHelper.ConvertToJpegFormat(imageBytes);

                            // Move artist image to artist folder
                            if (!doJustInfo)
                            {
                                File.WriteAllBytes(imageFilename, imageBytes);
                                artistImage.Delete();
                            }

                            Logger.LogDebug("Found Artist Image File [{0}], Moved to artist folder.", artistImage.Name);
                        }
                    }

                    // See if any secondary artist images if so then move to artist folder
                    artistImages.Clear();
                    artistImages.AddRange(ImageHelper.FindImageTypeInDirectory(fileInfo.Directory, ImageType.ArtistSecondary));
                    artistImages.AddRange(ImageHelper.FindImageTypeInDirectory(fileInfo.Directory.Parent, ImageType.Artist));
                    if (artistImages.Count > 0)
                    {
                        var looper = 0;
                        foreach (var artistImage in artistImages)
                        {
                            looper++;
                            var artistImageFilename = Path.Combine(artistFolder,
                                string.Format(ImageHelper.ArtistSecondaryImageFilename, looper.ToString("00")));
                            if (artistImageFilename != artistImage.FullName)
                            {
                                // Read image and convert to jpeg
                                var imageBytes = File.ReadAllBytes(artistImage.FullName);
                                imageBytes = ImageHelper.ConvertToJpegFormat(imageBytes);

                                // Move artist image to artist folder
                                if (!doJustInfo)
                                {
                                    while (File.Exists(artistImageFilename))
                                    {
                                        looper++;
                                        artistImageFilename = Path.Combine(artistFolder,
                                            string.Format(ImageHelper.ArtistSecondaryImageFilename,
                                                looper.ToString("00")));
                                    }

                                    File.WriteAllBytes(artistImageFilename, imageBytes);
                                    artistImage.Delete();
                                }

                                Logger.LogDebug(
                                    "Found Artist Secondary Image File [{0}], Moved to artist folder [{1}].",
                                    artistImage.Name, artistImageFilename);
                            }
                        }
                    }

                    // See if file folder has release image if so then move to release folder
                    var releaseImages = ImageHelper.FindImageTypeInDirectory(fileInfo.Directory, ImageType.Release);
                    if (releaseImages.Any())
                    {
                        var releaseImage = releaseImages.First();
                        var coverFileName = Path.Combine(releaseFolder, ImageHelper.ReleaseCoverFilename);
                        if (coverFileName != releaseImage.FullName)
                        {
                            // Read image and convert to jpeg
                            var imageBytes = File.ReadAllBytes(releaseImage.FullName);
                            imageBytes = ImageHelper.ConvertToJpegFormat(imageBytes);
                            if(imageBytes == null)
                            {
                                Logger.LogWarning($"Unable to read image [{ releaseImage.FullName }]");
                            }
                            // Move cover to release folder
                            if (!doJustInfo)
                            {
                                File.WriteAllBytes(coverFileName, imageBytes);
                                releaseImage.Delete();
                            }

                            Logger.LogTrace("Found Release Image File [{0}], Moved to release folder", releaseImage.Name);
                        }
                    }

                    // See if folder has secondary release image if so then move to release folder
                    releaseImages = ImageHelper.FindImageTypeInDirectory(fileInfo.Directory, ImageType.ReleaseSecondary);
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

                                Logger.LogTrace("Found Release Image File [{0}], Moved to release folder [{1}]", releaseImage.Name, releaseImageFilename);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error with Managing Images For [{fileInfo.FullName}], ImageFilename [{imageFilename }]");
                }

                var doesFileExistsForTrack = File.Exists(destinationName);
                if (doesFileExistsForTrack)
                {
                    var existing = new FileInfo(destinationName);

                    // If Exists determine which is better - if same do nothing
                    var existingMetaData = await AudioMetaDataHelper.GetInfo(existing, doJustInfo).ConfigureAwait(false);

                    var areSameFile = existing.FullName.Replace("\\", "").Replace("/", "")
                        .Equals(fileInfo.FullName.Replace("\\", "").Replace("/", ""),
                            StringComparison.OrdinalIgnoreCase);
                    var currentBitRate = metaData.AudioBitrate;
                    var existingBitRate = existingMetaData.AudioBitrate;

                    if (!areSameFile)
                    {
                        if (!existingMetaData.IsValid || currentBitRate > existingBitRate)
                        {
                            Logger.LogTrace("Newer Is Better: Deleting Existing File [{0}]", existing);
                            if (!doJustInfo)
                            {
                                existing.Delete();
                                fileInfo.MoveTo(destinationName);
                            }
                        }
                        else
                        {
                            Logger.LogTrace("Existing [{0}] Is Better or Equal: Deleting Found File [{1}]", existing,
                                fileInfo.FullName);
                            if (!doJustInfo) fileInfo.Delete();
                        }
                    }
                }
                else
                {
                    Logger.LogTrace("Moving File To [{0}]", destinationName);
                    if (!doJustInfo)
                    {
                        try
                        {
                            fileInfo.MoveTo(destinationName);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Error Moving File [{0}}", destinationName);
                        }
                    }
                }

                result.AdditionalData.Add(PluginResultInfo.AdditionalDataKeyPluginResultInfo, new PluginResultInfo
                {
                    ArtistFolder = artistFolder,
                    ArtistId = _artistId,
                    ReleaseFolder = releaseFolder,
                    ReleaseId = _releaseId,
                    Filename = fileInfo.FullName,
                    TrackNumber = metaData.TrackNumber,
                    TrackTitle = metaData.Title
                });
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error Processing File [{0}}", fileInfo);
            }

            sw.Stop();
            Logger.LogTrace("<< Audio: Process Complete. Result `{0}`, ElapsedTime [{1}]", CacheManager.CacheSerializer.Serialize(result), sw.ElapsedMilliseconds);
            return result;
        }

        private async Task<string> DetermineArtistFolder(AudioMetaData metaData,
            bool doJustInfo)
        {
            var artist = await ArtistLookupEngine.GetByName(metaData, !doJustInfo).ConfigureAwait(false);
            if (!artist.IsSuccess) return null;
            try
            {
                return artist.Data.ArtistFileFolder(Configuration);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Serialize());
            }

            return null;
        }

        private async Task<string> DetermineReleaseFolder(string artistFolder, AudioMetaData metaData, bool doJustInfo,
            int? submissionId)
        {
            var artist = await ArtistLookupEngine.GetByName(metaData, !doJustInfo).ConfigureAwait(false);
            if (!artist.IsSuccess)
            {
                return null;
            }
            _artistId = artist.Data.RoadieId;
            var release = await ReleaseLookupEngine.GetByName(artist.Data, metaData, !doJustInfo, submissionId: submissionId).ConfigureAwait(false);
            if (!release.IsSuccess)
            {
                return null;
            }
            _releaseId = release.Data.RoadieId;
            release.Data.ReleaseDate = SafeParser.ToDateTime(release.Data.ReleaseYear ?? metaData.Year);
            if (release.Data.ReleaseYear.HasValue && release.Data.ReleaseYear != metaData.Year)
            {
                Logger.LogWarning($"Found Release `{release.Data}` has different Release Year than MetaData Year `{metaData}`");
            }
            return release.Data.ReleaseFileFolder(artistFolder);
        }
    }
}