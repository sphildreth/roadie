using Roadie.Library.Caching;
using Roadie.Library.Extensions;
using Roadie.Library.Factories;
using Roadie.Library.Utility;
using Roadie.Library.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Roadie.Library.MetaData.MusicBrainz;
using Microsoft.Extensions.Configuration;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.MetaData.LastFm;
using Roadie.Library.Imaging;
using Roadie.Library.Encoding;

namespace Roadie.Library.FilePlugins
{
    public class Audio : PluginBase
    {
        private Guid _releaseId = Guid.Empty;
        private Guid _artistId = Guid.Empty;

        private MusicBrainzProvider _musicBrainzProvider = null;

        public MusicBrainzProvider MusicBrainzProvider
        {
            get
            {
                return this._musicBrainzProvider ?? (this._musicBrainzProvider = new MusicBrainzProvider(this.Configuration, this.CacheManager, this.Logger));
            }
            set
            {
                this._musicBrainzProvider = value;
            }
        }

        private LastFmHelper _lastFmHelper = null;

        public LastFmHelper LastFmHelper
        {
            get
            {
                return this._lastFmHelper ?? (this._lastFmHelper = new LastFmHelper(this.Configuration, this.CacheManager, this.Logger));
            }
            set
            {
                this._lastFmHelper = value;
            }
        }

        private AudioMetaDataHelper _audioMetaDataHelper = null;

        public AudioMetaDataHelper AudioMetaDataHelper
        {
            get
            {
                return this._audioMetaDataHelper ?? (this._audioMetaDataHelper = new AudioMetaDataHelper(this.Configuration, this.HttpEncoder, null, this.MusicBrainzProvider, this.LastFmHelper, this.CacheManager, this.Logger, this.ImageFactory));
            }
            set
            {
                this._audioMetaDataHelper = value;
            }
        }

        public override string[] HandlesTypes
        {
            get
            {
                return new string[2] { "audio/mpeg", "text/json" };
            }
        }


        public Audio(IConfiguration configuration, 
            IHttpEncoder httpEncoder,
            ArtistFactory artistFactory,
            ReleaseFactory releaseFactory,
            ImageFactory imageFactory,
            ICacheManager cacheManager,
            ILogger logger) : base(configuration, httpEncoder, artistFactory, releaseFactory, imageFactory, cacheManager, logger)
        {
        }

        public override async Task<OperationResult<bool>> Process(string destinationRoot, FileInfo fileInfo, bool doJustInfo, int? submissionId)
        {
            var dr = destinationRoot ?? fileInfo.DirectoryName;
            var result = new OperationResult<bool>();

            string destinationName = null;

            var metaData = await this.AudioMetaDataHelper.GetInfo(fileInfo, doJustInfo);
            if (!metaData.IsValid)
            {
                var minWeight = this.MinWeightToDelete;
                if (metaData.ValidWeight < minWeight && minWeight > 0)
                {
                    this.Logger.Trace("Invalid File{3}: ValidWeight [{0}], Under MinWeightToDelete [{1}]. Deleting File [{2}]", metaData.ValidWeight, minWeight, fileInfo.FullName, doJustInfo ? " [Read Only Mode] " : string.Empty);
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
                this.Logger.Warning("Unable To Find ArtistFolder [{0}] For MetaData [{1}]", artistFolder, metaData.ToString());
                return new OperationResult<bool>
                {
                    Messages = new List<string> { "Unable To Find Artist Folder" }
                };
            }
            var releaseFolder = await this.DetermineReleaseFolder(artistFolder, metaData, doJustInfo, submissionId);
            if (string.IsNullOrEmpty(releaseFolder))
            {
                this.Logger.Warning("Unable To Find ReleaseFolder For MetaData [{0}]", metaData.ToString());
                return new OperationResult<bool>
                {
                    Messages = new List<string> { "Unable To Find Release Folder" }
                };
            }
            destinationName = FolderPathHelper.TrackFullPath(this.Configuration, metaData, dr, artistFolder);
            this.Logger.Trace("Info: FileInfo [{0}], Artist Folder [{1}], Destination Name [{2}]", fileInfo.FullName, artistFolder, destinationName);

            if (doJustInfo)
            {
                result.IsSuccess = metaData.IsValid;
                return result;
            }

            PluginBase.CheckMakeFolder(artistFolder);
            PluginBase.CheckMakeFolder(releaseFolder);

            // See if folder has "cover" image if so then move to release folder for metadata
            var imageFiles = ImageHelper.ImageFilesInFolder(fileInfo.DirectoryName);
            if (imageFiles != null && imageFiles.Any())
            {
                foreach (var imageFile in imageFiles)
                {
                    var i = new FileInfo(imageFile);
                    var iName = i.Name.ToLower().Trim();
                    this.Logger.Debug("Found Image File [{0}] [{1}]", imageFile, iName);
                    var isCoverArtType = iName.StartsWith("cover") || iName.StartsWith("folder") || iName.StartsWith("front") || iName.StartsWith("release") || iName.StartsWith("album");
                    if (isCoverArtType)
                    {
                        var coverFileName = Path.Combine(releaseFolder, ReleaseFactory.CoverFilename);
                        if (coverFileName != i.FullName)
                        {
                            // Read image and convert to jpeg
                            var imageBytes = File.ReadAllBytes(i.FullName);
                            imageBytes = ImageHelper.ConvertToJpegFormat(imageBytes);

                            // Move cover to release folder 
                            if (!doJustInfo)
                            {
                                File.WriteAllBytes(coverFileName, imageBytes);
                                i.Delete();
                            }
                            this.Logger.Debug("Found Image File [{0}], Moved to release folder", i.Name);
                            break;
                        }
                    }
                }
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
                        this.Logger.Trace("Newer Is Better: Deleting Existing File [{0}]", existing);
                        if (!doJustInfo)
                        {
                            existing.Delete();
                            fileInfo.MoveTo(destinationName);
                        }
                    }
                    else
                    {
                        this.Logger.Trace("Existing [{0}] Is Better or Equal: Deleting Found File [{1}]", existing, fileInfo.FullName);
                        if (!doJustInfo)
                        {
                            fileInfo.Delete();
                        }
                    }
                }
            }
            else
            {
                this.Logger.Trace("Moving File To [{0}]", destinationName);
                if (!doJustInfo)
                {
                    fileInfo.MoveTo(destinationName);
                }
            }

            result.IsSuccess = true;
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
            return result;
        }

        private async Task<string> DetermineReleaseFolder(string artistFolder, AudioMetaData metaData, bool doJustInfo, int? submissionId)
        {
            var artist = await this.ArtistFactory.GetByName(metaData, !doJustInfo);
            if (!artist.IsSuccess)
            {
                return null;
            }
            this._artistId = artist.Data.RoadieId;
            var release = await this.ReleaseFactory.GetByName(artist.Data, metaData, !doJustInfo, submissionId: submissionId);
            if (!release.IsSuccess)
            {
                return null;
            }
            this._releaseId = release.Data.RoadieId;
            release.Data.ReleaseDate = SafeParser.ToDateTime(metaData.Year);
            return release.Data.ReleaseFileFolder(artistFolder);
        }

        private async Task<string> DetermineArtistFolder(string destinationRoot, AudioMetaData metaData, bool doJustInfo)
        {
            var artist = await this.ArtistFactory.GetByName(metaData, !doJustInfo);
            if (!artist.IsSuccess)
            {
                return null;
            }
            try
            {
                return artist.Data.ArtistFileFolder(this.Configuration,destinationRoot);
            }
            catch (Exception ex)
            {
                this._loggingService.Error(ex, ex.Serialize());
            }
            return null;
        }
    }
}