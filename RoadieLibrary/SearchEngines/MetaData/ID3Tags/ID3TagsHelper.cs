using Microsoft.Extensions.Logging;
using Roadie.Library.Caching;
using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.MetaData.Audio;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using IdSharp.AudioInfo;
using IdSharp.Common.Utils;
using IdSharp.Tagging.ID3v1;
using IdSharp.Tagging.ID3v2;
using Newtonsoft.Json;

namespace Roadie.Library.MetaData.ID3Tags
{
    public class ID3TagsHelper : MetaDataProviderBase
    {
        public ID3TagsHelper(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger)
            : base(configuration, cacheManager, logger)
        {
        }

        public OperationResult<AudioMetaData> MetaDataForFile(string fileName)
        {
            var result = this.MetaDataForFileFromIdSharp(fileName);
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
                var r = this.MetaDataForFileFromIdSharp(fileName);
                if (r.IsSuccess)
                {
                    result.Add(r.Data);
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
                // TODO 

                //var tagFile = TagLib.File.Create(filename);
                //tagFile.Tag.AlbumArtists = null;
                //tagFile.Tag.AlbumArtists = new[] { metaData.Artist };
                //tagFile.Tag.Performers = null;
                //if (metaData.TrackArtists.Any())
                //{
                //    tagFile.Tag.Performers = metaData.TrackArtists.ToArray();
                //}
                //tagFile.Tag.Album = metaData.Release;
                //tagFile.Tag.Title = metaData.Title;
                //tagFile.Tag.Year = force ? (uint)(metaData.Year ?? 0) : tagFile.Tag.Year > 0 ? tagFile.Tag.Year : (uint)(metaData.Year ?? 0);
                //tagFile.Tag.Track = force ? (uint)(metaData.TrackNumber ?? 0) : tagFile.Tag.Track > 0 ? tagFile.Tag.Track : (uint)(metaData.TrackNumber ?? 0);
                //tagFile.Tag.TrackCount = force ? (uint)(metaData.TotalTrackNumbers ?? 0) : tagFile.Tag.TrackCount > 0 ? tagFile.Tag.TrackCount : (uint)(metaData.TotalTrackNumbers ?? 0);
                //tagFile.Tag.Disc = force ? (uint)(metaData.Disk ?? 0) : tagFile.Tag.Disc > 0 ? tagFile.Tag.Disc : (uint)(metaData.Disk ?? 0);
                //tagFile.Tag.Pictures = metaData.Images == null ? null : metaData.Images.Select(x => new TagLib.Picture
                //{
                //    Data = new TagLib.ByteVector(x.Data),
                //    Description = x.Description,
                //    MimeType = x.MimeType,
                //    Type = (TagLib.PictureType)x.Type
                //}).ToArray();
                //tagFile.Save();
                return true;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, string.Format("MetaData [{0}], Filename [{1}]", metaData.ToString(), filename));
            }
            return false;
        }


        private OperationResult<AudioMetaData> MetaDataForFileFromIdSharp(string fileName)
        {
            var sw = new Stopwatch();
            sw.Start();
            AudioMetaData result = new AudioMetaData();
            var isSuccess = false;
            try
            {
                IAudioFile audioFile = AudioFile.Create(fileName, true);
                if (ID3v2Tag.DoesTagExist(fileName))
                {
                    IID3v2Tag id3v2 = new ID3v2Tag(fileName);
                    result.Release = id3v2.Album;
                    result.Artist = id3v2.AlbumArtist ?? id3v2.Artist;
                    result.ArtistRaw = id3v2.AlbumArtist ?? id3v2.Artist;
                    result.Genres = id3v2.Genre?.Split(new char[] { ',', '\\' });
                    result.TrackArtist = id3v2.OriginalArtist ?? id3v2.Artist ?? id3v2.AlbumArtist;
                    result.TrackArtistRaw = id3v2.OriginalArtist;
                    result.AudioBitrate = (int?)audioFile.Bitrate;
                    result.AudioChannels = audioFile.Channels;
                    result.AudioSampleRate = (int)audioFile.Bitrate;
                    result.Disk = SafeParser.ToNumber<int?>(id3v2.DiscNumber);
                    result.Images = id3v2.PictureList?.Select(x => new AudioMetaDataImage
                    {
                        Data = x.PictureData,
                        Description = x.Description,
                        MimeType = x.MimeType,
                        Type = (AudioMetaDataImageType)x.PictureType
                    }).ToArray();
                    result.Time = (int)audioFile.TotalSeconds > 0 ? ((decimal?)audioFile.TotalSeconds).ToTimeSpan() : null;
                    result.Title = id3v2.Title.ToTitleCase(false);

                    var trackparts = id3v2.TrackNumber?.Split('/');
                    result.TrackNumber = SafeParser.ToNumber<short?>(trackparts[0]);
                    result.TotalTrackNumbers = trackparts.Length > 1 ? SafeParser.ToNumber<short?>(trackparts[1]) : 0;
                    result.Year = SafeParser.ToNumber<int?>(id3v2.Year);
                    isSuccess = true;
                }

                if (!isSuccess)
                {
                    if (ID3v1Tag.DoesTagExist(fileName))
                    {
                        IID3v1Tag id3v1 = new ID3v1Tag(fileName);
                        result.Release = id3v1.Album;
                        result.Artist = id3v1.Artist;
                        result.ArtistRaw = id3v1.Artist;
                        result.AudioBitrate = (int?)audioFile.Bitrate;
                        result.AudioChannels = audioFile.Channels;
                        result.AudioSampleRate = (int)audioFile.Bitrate;
                        result.Time = (int)audioFile.TotalSeconds > 0 ? ((decimal?)audioFile.TotalSeconds).ToTimeSpan() : null;
                        result.Title = id3v1.Title.ToTitleCase(false);

                        result.TrackNumber = (short?)id3v1.TrackNumber;
                        result.Year = SafeParser.ToNumber<int?>(id3v1.Year);
                        isSuccess = true;
                    }
                }

            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "MetaDataForFileFromTagLib Filename [" + fileName + "] Error [" + ex.Serialize() + "]");
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