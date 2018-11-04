using Orthogonal.NTagLite;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.Logging;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Roadie.Library.MetaData.ID3Tags
{
    public class ID3TagsHelper : MetaDataProviderBase
    {
        public ID3TagsHelper(IRoadieSettings configuration, ICacheManager cacheManager, ILogger loggingService)
            : base(configuration, cacheManager, loggingService)
        {
        }

        public OperationResult<AudioMetaData> MetaDataForFile(string fileName)
        {
            var result = this.MetaDataForFileFromTagLib(fileName);
            if (result.IsSuccess)
            {
                return result;
            }
            result = this.MetaDataForFileFromNTagLite(fileName);
            if (result.IsSuccess)
            {
                return result;
            }
            return new OperationResult<AudioMetaData>();
        }

        public OperationResult<IEnumerable<AudioMetaData>> MetaDataForFiles(IEnumerable<string> fileNames)
        {
            var result = new List<AudioMetaData>();
            foreach (var fileName in fileNames)
            {
                var r = this.MetaDataForFileFromTagLib(fileName);
                if (r.IsSuccess)
                {
                    result.Add(r.Data);
                }
                else
                {
                    r = this.MetaDataForFileFromNTagLite(fileName);
                    if (r.IsSuccess)
                    {
                        result.Add(r.Data);
                    }
                }
            }
            return new OperationResult<IEnumerable<AudioMetaData>>
            {
                IsSuccess = result.Any(),
                Data = result
            };
        }

        public OperationResult<IEnumerable<AudioMetaData>> MetaDataForFolder(string folderName)
        {
            return this.MetaDataForFiles(Directory.EnumerateFiles(folderName, "*.mp3", SearchOption.AllDirectories).ToArray());
        }

        public bool WriteTags(AudioMetaData metaData, string filename, bool force = false)
        {
            try
            {
                var tagFile = TagLib.File.Create(filename);
                tagFile.Tag.AlbumArtists = null;
                tagFile.Tag.AlbumArtists = new[] { metaData.Artist };
                tagFile.Tag.Performers = null;
                if (metaData.TrackArtists.Any())
                {
                    tagFile.Tag.Performers = metaData.TrackArtists.ToArray();
                }
                tagFile.Tag.Album = metaData.Release;
                tagFile.Tag.Title = metaData.Title;
                tagFile.Tag.Year = force ? (uint)(metaData.Year ?? 0) : tagFile.Tag.Year > 0 ? tagFile.Tag.Year : (uint)(metaData.Year ?? 0);
                tagFile.Tag.Track = force ? (uint)(metaData.TrackNumber ?? 0) : tagFile.Tag.Track > 0 ? tagFile.Tag.Track : (uint)(metaData.TrackNumber ?? 0);
                tagFile.Tag.TrackCount = force ? (uint)(metaData.TotalTrackNumbers ?? 0) : tagFile.Tag.TrackCount > 0 ? tagFile.Tag.TrackCount : (uint)(metaData.TotalTrackNumbers ?? 0);
                tagFile.Tag.Disc = force ? (uint)(metaData.Disk ?? 0) : tagFile.Tag.Disc > 0 ? tagFile.Tag.Disc : (uint)(metaData.Disk ?? 0);
                tagFile.Tag.Pictures = metaData.Images == null ? null : metaData.Images.Select(x => new TagLib.Picture
                {
                    Data = new TagLib.ByteVector(x.Data),
                    Description = x.Description,
                    MimeType = x.MimeType,
                    Type = (TagLib.PictureType)x.Type
                }).ToArray();
                tagFile.Save();
                return true;
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex, string.Format("MetaData [{0}], Filename [{1}]", metaData.ToString(), filename));
            }
            return false;
        }

        private OperationResult<AudioMetaData> MetaDataForFileFromNTagLite(string fileName)
        {
            var sw = new Stopwatch();
            sw.Start();
            AudioMetaData result = new AudioMetaData();
            var isSuccess = false;
            try
            {
                var file = LiteFile.LoadFromFile(fileName);
                var tpos = file.Tag.FindFirstFrameById(FrameId.TPOS);
                Picture[] pics = file.Tag.FindFramesById(FrameId.APIC).Select(f => f.GetPicture()).ToArray();
                result.Release = file.Tag.Album;
                result.Artist = file.Tag.Artist;
                result.ArtistRaw = file.Tag.Artist;
                result.Genres = (file.Tag.Genre ?? string.Empty).Split(';');
                result.TrackArtist = file.Tag.OriginalArtist;
                result.TrackArtistRaw = file.Tag.OriginalArtist;
                result.AudioBitrate = file.Bitrate;
                result.AudioChannels = file.AudioMode.HasValue ? (int?)file.AudioMode.Value : null;
                result.AudioSampleRate = file.Frequency;
                result.Disk = tpos != null ? SafeParser.ToNumber<int?>(tpos.Text) : null;
                result.Images = pics.Select(x => new AudioMetaDataImage
                {
                    Data = x.Data,
                    Description = x.Description,
                    MimeType = x.MimeType,
                    Type = (AudioMetaDataImageType)x.PictureType
                }).ToArray();
                result.Time = file.Duration;
                result.Title = file.Tag.Title.ToTitleCase(false);
                result.TotalTrackNumbers = file.Tag.TrackCount;
                result.TrackNumber = file.Tag.TrackNumber;
                result.Year = file.Tag.Year;
                isSuccess = true;
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex, "MetaDataForFileFromTagLib Filename [" + fileName + "] Error [" + ex.Serialize() + "]");
            }
            sw.Stop();
            return new OperationResult<AudioMetaData>
            {
                IsSuccess = isSuccess,
                OperationTime = sw.ElapsedMilliseconds,
                Data = result
            };
        }

        private OperationResult<AudioMetaData> MetaDataForFileFromTagLib(string fileName)
        {
            var sw = new Stopwatch();
            sw.Start();
            AudioMetaData result = new AudioMetaData();
            var isSuccess = false;
            try
            {
                var tagFile = TagLib.File.Create(fileName);
                result.Release = tagFile.Tag.Album;
                result.Artist = !string.IsNullOrEmpty(tagFile.Tag.JoinedAlbumArtists) ? tagFile.Tag.JoinedAlbumArtists : tagFile.Tag.JoinedPerformers;
                result.ArtistRaw = !string.IsNullOrEmpty(tagFile.Tag.JoinedAlbumArtists) ? tagFile.Tag.JoinedAlbumArtists : tagFile.Tag.JoinedPerformers;
                result.Genres = tagFile.Tag.Genres != null ? tagFile.Tag.Genres : new string[0];
                result.TrackArtist = tagFile.Tag.JoinedPerformers;
                result.TrackArtistRaw = tagFile.Tag.JoinedPerformers;
                result.AudioBitrate = (tagFile.Properties.AudioBitrate > 0 ? (int?)tagFile.Properties.AudioBitrate : null);
                result.AudioChannels = (tagFile.Properties.AudioChannels > 0 ? (int?)tagFile.Properties.AudioChannels : null);
                result.AudioSampleRate = (tagFile.Properties.AudioSampleRate > 0 ? (int?)tagFile.Properties.AudioSampleRate : null);
                result.Disk = (tagFile.Tag.Disc > 0 ? (int?)tagFile.Tag.Disc : null);
                result.Images = (tagFile.Tag.Pictures != null ? tagFile.Tag.Pictures.Select(x => new AudioMetaDataImage
                {
                    Data = x.Data.Data,
                    Description = x.Description,
                    MimeType = x.MimeType,
                    Type = (AudioMetaDataImageType)x.Type
                }).ToArray() : null);
                result.Time = (tagFile.Properties.Duration.TotalMinutes > 0 ? (TimeSpan?)tagFile.Properties.Duration : null);
                result.Title = tagFile.Tag.Title.ToTitleCase(false);
                result.TotalTrackNumbers = (tagFile.Tag.TrackCount > 0 ? (int?)tagFile.Tag.TrackCount : null);
                result.TrackNumber = (tagFile.Tag.Track > 0 ? (short?)tagFile.Tag.Track : null);
                result.Year = (tagFile.Tag.Year > 0 ? (int?)tagFile.Tag.Year : null);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex, "MetaDataForFileFromTagLib Filename [" + fileName + "] Error [" + ex.Serialize() + "]");
            }
            sw.Stop();
            return new OperationResult<AudioMetaData>
            {
                IsSuccess = isSuccess,
                OperationTime = sw.ElapsedMilliseconds,
                Data = result
            };
        }
    }
}