using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Encoding;
using Roadie.Library.Engines;
using Roadie.Library.Extensions;
using Roadie.Library.Factories;
using Roadie.Library.MetaData.FileName;
using Roadie.Library.MetaData.ID3Tags;
using Roadie.Library.MetaData.LastFm;
using Roadie.Library.MetaData.MusicBrainz;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Roadie.Library.MetaData.Audio
{
    public sealed class AudioMetaDataHelper : IAudioMetaDataHelper
    {
        private IArtistLookupEngine ArtistLookupEngine { get; }

        private IntPtr nativeResource = Marshal.AllocHGlobal(100);

        public bool DoParseFromDiscogsDB { get; set; }

        public bool DoParseFromDiscogsDBFindingTrackForArtist { get; set; }

        public bool DoParseFromFileName { get; set; }

        public bool DoParseFromLastFM { get; set; }

        public bool DoParseFromMusicBrainz { get; set; }

        private ICacheManager CacheManager { get; }

        private IRoadieSettings Configuration { get; }

        private IFileNameHelper FileNameHelper { get; } 

        private IHttpEncoder HttpEncoder { get; }

        private IID3TagsHelper ID3TagsHelper { get; }

        private IImageFactory ImageFactory { get; }

        private ILastFmHelper LastFmHelper { get; }

        private ILogger Logger { get; }

        private IMusicBrainzProvider MusicBrainzProvider { get; }

        public AudioMetaDataHelper(IRoadieSettings configuration, IHttpEncoder httpEncoder, IRoadieDbContext context, IMusicBrainzProvider musicBrainzHelper, 
                                   ILastFmHelper lastFmHelper, ICacheManager cacheManager, ILogger logger, IArtistLookupEngine artistLookupEngine, 
                                   IImageFactory imageFactory, IFileNameHelper filenameHelper, IID3TagsHelper id3TagsHelper)
        {
            this.Configuration = configuration;
            this.HttpEncoder = httpEncoder;
            this.CacheManager = cacheManager;
            this.Logger = logger;
            this.ImageFactory = ImageFactory;
            this.FileNameHelper = filenameHelper;
            this.ID3TagsHelper = id3TagsHelper;

            this.MusicBrainzProvider = musicBrainzHelper;
            this.LastFmHelper = lastFmHelper;

            this.ArtistLookupEngine = artistLookupEngine;

            this.DoParseFromFileName = configuration.Processing.DoParseFromFileName;
            this.DoParseFromDiscogsDBFindingTrackForArtist = configuration.Processing.DoParseFromDiscogsDBFindingTrackForArtist;
            this.DoParseFromDiscogsDB = configuration.Processing.DoParseFromDiscogsDB;
            this.DoParseFromMusicBrainz = configuration.Processing.DoParseFromMusicBrainz;
            this.DoParseFromLastFM = configuration.Processing.DoParseFromLastFM;
        }


        /// <summary>
        /// For the given File extract out all the information if successfully pulled out then return true
        /// </summary>
        /// <param name="fileInfo">FileInfo to Process</param>
        /// <param name="doJustInfo">Toggle To Only Print Info Not Modify Files</param>
        /// <returns>If parsing information for File was successful</returns>
        public async Task<AudioMetaData> GetInfo(FileInfo fileInfo, bool doJustInfo = false)
        {
            var tagSources = new List<string> { "Tags" };
            var result = this.ParseFromTags(fileInfo);
            result.Filename = fileInfo.FullName;
            if (!result.IsValid)
            {
                tagSources.Add("Filename");
                result = this.ParseFromFilename(result, fileInfo);
                if (string.IsNullOrEmpty(result.Artist) || string.IsNullOrEmpty(result.Release))
                {
                    if (string.IsNullOrEmpty(result.Artist) || string.IsNullOrEmpty(result.Release))
                    {
                        this.Logger.LogWarning("File [{0}] MetaData [{1}]: Unable to Determine Artist and Release; aborting getting info.", fileInfo.FullName, result.ToString());
                        return result;
                    }
                }
                if (!result.IsValid)
                {
                    if (!result.IsValid)
                    {
                        tagSources.Add("MusicBrainz");
                        result = await this.ParseFromMusicBrainz(result);
                        if (!result.IsValid)
                        {
                            tagSources.Add("LastFm");
                            result = await this.GetFromLastFmIntegration(result);
                        }
                    }
                }
                if (!result.IsValid)
                {
                    this.Logger.LogWarning("File [{0}] MetaData Invalid, TagSources [{1}] MetaData [{2}]", fileInfo.FullName, string.Join(",", tagSources), result.ToString());
                }
                else
                {
                    if (result.IsValid && !doJustInfo)
                    {
                        if (result.Images == null || !result.Images.Any())
                        {
                            var imageMetaData = this.ImageFactory.GetPictureForMetaData(fileInfo.FullName, result);
                            var tagImages = imageMetaData == null ? null : new List<AudioMetaDataImage> { imageMetaData };
                            result.Images = tagImages != null && tagImages.Any() ? tagImages : null;
                            if (result.Images == null || !result.Images.Any())
                            {
                                this.Logger.LogTrace("File [{0} No Images Set and Unable to Find Images", fileInfo.FullName);
                            }
                        }
                        this.WriteTags(result, fileInfo);
                    }
                }
            }
            var artistNameReplacements = this.Configuration.ArtistNameReplace;
            if (artistNameReplacements != null)
            {
                var artistNameReplaceKp = artistNameReplacements.FirstOrDefault(x => x.Value.Any(v => v.Equals(result.ArtistRaw, StringComparison.OrdinalIgnoreCase)));
                if (artistNameReplaceKp.Key != null && artistNameReplaceKp.Key != result.Artist)
                {
                    result.SetArtistName(artistNameReplaceKp.Key);
                }
            }
            this.Logger.LogInformation("File [{0}], TagSources [{1}] MetaData [{2}]", fileInfo.Name, string.Join(",", tagSources), result.ToString());
            return result;
        }

        public bool WriteTags(AudioMetaData metaData, FileInfo fileInfo)
        {
            return this.ID3TagsHelper.WriteTags(metaData, fileInfo.FullName);
        }

        private static AudioMetaData MergeAudioData(IRoadieSettings settings, AudioMetaData left, AudioMetaData right)
        {
            var result = new AudioMetaData();
            if (left == null)
            {
                return right;
            }
            if (right == null)
            {
                return left;
            }
            result.Release = left.Release.Or(right.Release).SafeReplace("_").SafeReplace("~", ",").CleanString(settings);
            result.ArtistRaw = left.ArtistRaw.Or(right.ArtistRaw);
            result.TrackArtistRaw = left.TrackArtistRaw.Or(right.TrackArtistRaw);
            result.Artist = left.Artist.Or(right.Artist).SafeReplace("_").SafeReplace("~", ",").CleanString(settings);
            result.Title = left.Title.Or(right.Title).SafeReplace("_").SafeReplace("~", ",").CleanString(settings);
            result.Year = left.Year.Or(right.Year);
            result.TrackNumber = left.TrackNumber.Or(right.TrackNumber);
            result.TotalTrackNumbers = left.TotalTrackNumbers.Or(right.TotalTrackNumbers);
            result.Disk = left.Disk.Or(right.Disk);
            result.Time = left.Time ?? right.Time;
            result.AudioBitrate = left.AudioBitrate.Or(right.AudioBitrate);
            result.AudioChannels = left.AudioChannels.Or(right.AudioChannels);
            result.AudioSampleRate = left.AudioSampleRate.Or(right.AudioSampleRate);
            if (left.Images != null && right.Images == null)
            {
                result.Images = left.Images;
            }
            else if (left.Images == null && right.Images != null)
            {
                result.Images = right.Images;
            }
            else if (left.Images != null && right.Images != null)
            {
                result.Images = left.Images.Union(right.Images);
            }
            return result;
        }

        private async Task<AudioMetaData> GetFromLastFmIntegration(AudioMetaData metaData)
        {
            var artistName = metaData.Artist;
            var ReleaseName = metaData.Release;

            if (this.DoParseFromLastFM)
            {
                if (string.IsNullOrEmpty(artistName) && string.IsNullOrEmpty(ReleaseName))
                {
                    return metaData;
                }
                var lastFmReleaseTracks = await this.LastFmHelper.TracksForRelease(artistName, ReleaseName);
                if (lastFmReleaseTracks != null)
                {
                    var lastFmReleaseTrack = lastFmReleaseTracks.FirstOrDefault(x => x.TrackNumber == metaData.TrackNumber || x.Title.Equals(metaData.Title, StringComparison.InvariantCultureIgnoreCase));
                    if (lastFmReleaseTrack != null)
                    {
                        return MergeAudioData(this.Configuration, metaData, lastFmReleaseTrack);
                    }
                }
            }
            return metaData;
        }

        private AudioMetaData ParseFromFilename(AudioMetaData metaData, FileInfo fileInfo)
        {
            if (this.DoParseFromFileName)
            {
                var filename = fileInfo.Name.Replace(fileInfo.Extension, "");
                var mdFromFilename = this.FileNameHelper.MetaDataFromFilename(filename);
                if (mdFromFilename.ValidWeight < 32)
                {
                    var mdFromFileInfo = FileNameHelper.MetaDataFromFileInfo(fileInfo);
                    if (mdFromFileInfo.ValidWeight > mdFromFilename.ValidWeight)
                    {
                        mdFromFilename = mdFromFileInfo;
                    }
                }
                if ((mdFromFilename.Year ?? 0) < 1)
                {
                    mdFromFilename.Year = SafeParser.ToYear(fileInfo.Directory.Name.Substring(0, 4));
                }
                return MergeAudioData(this.Configuration, metaData, mdFromFilename);
            }
            return metaData;
        }

        private async Task<AudioMetaData> ParseFromMusicBrainz(AudioMetaData metaData)
        {
            if (this.DoParseFromMusicBrainz)
            {
                var musicBrainzReleaseTracks = await this.MusicBrainzProvider.MusicBrainzReleaseTracks(metaData.Artist, metaData.Release);
                if (musicBrainzReleaseTracks != null)
                {
                    var musicBrainzReleaseTrack = musicBrainzReleaseTracks.FirstOrDefault(x => x.TrackNumber == metaData.TrackNumber || x.Title.Equals(metaData.Title, StringComparison.InvariantCultureIgnoreCase));
                    if (musicBrainzReleaseTrack != null)
                    {
                        return MergeAudioData(this.Configuration, metaData, musicBrainzReleaseTrack);
                    }
                }
            }
            return metaData;
        }

        private AudioMetaData ParseFromTags(FileInfo fileInfo)
        {
            try
            {
                var metaDataFromFile = this.ID3TagsHelper.MetaDataForFile(fileInfo.FullName);
                if (metaDataFromFile.IsSuccess)
                {
                    return metaDataFromFile.Data;
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, string.Format("Error With ID3TagsHelper.MetaDataForFile From File [{0}]", fileInfo.FullName));
            }
            return new AudioMetaData
            {
                Filename = fileInfo.FullName
            };
        }
    }
}