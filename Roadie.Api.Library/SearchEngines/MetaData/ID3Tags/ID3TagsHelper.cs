using ATL;
using ATL.CatalogDataReaders;
using ATL.PlaylistReaders;
using IdSharp.AudioInfo;
using IdSharp.Tagging.ID3v1;
using IdSharp.Tagging.ID3v2;
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
using System.Text.RegularExpressions;

namespace Roadie.Library.MetaData.ID3Tags
{
    public class ID3TagsHelper : MetaDataProviderBase, IID3TagsHelper
    {
        public ID3TagsHelper(IRoadieSettings configuration, ICacheManager cacheManager, ILogger logger)
            : base(configuration, cacheManager, logger)
        {
        }

        public static int DetermineDiscNumber(AudioMetaData metaData)
        {
            var maxDiscNumber = 500; // Damnit Karajan
            for (var i = maxDiscNumber; i > 0; i--)
            {
                if (Regex.IsMatch(metaData.Filename, @"(cd\s*(0*" + i + "))", RegexOptions.IgnoreCase))
                {
                    return i;
                }
            }
            return 1;
        }

        public static string DetermineMissingRequiredMetaData(AudioMetaData metaData)
        {
            var result = new List<string>();
            if(string.IsNullOrEmpty(metaData.Artist))
            {
                result.Add("Artist Name (TPE1)");
            }
            if(string.IsNullOrEmpty(metaData.Release))
            {
                result.Add("Release Title (TALB)");
            }
            if (string.IsNullOrEmpty(metaData.Title))
            {
                result.Add("Track Title (TIT2)");
            }
            if ((metaData.Year ?? 0) < 1)
            {
                result.Add("Release Year (TYER | TDRC | TORY | TDOR)");
            }
            if ((metaData.TrackNumber ?? 0) < 1)
            {
                result.Add("TrackNumber (TRCK)");
            }
            return string.Join(",", result);
        }

        public static int DetermineTotalDiscNumbers(IEnumerable<AudioMetaData> metaDatas)
        {
            var result = 1;
            foreach (var metaData in metaDatas.OrderBy(x => x.Filename))
            {
                var n = DetermineDiscNumber(metaData);
                result = result > n ? result : n;
            }
            return result;
        }

        public static short? DetermineTotalTrackNumbers(string filename, string trackNumber = null)
        {
            short? result = null;
            if (!string.IsNullOrEmpty(filename))
            {
                var fileInfo = new FileInfo(filename);
                var directoryName = fileInfo.DirectoryName;

                // See if CUE sheet exists if so read tracks from that and return latest track number
                var cueFiles = Directory.GetFiles(directoryName, ("*.cue"));
                if (cueFiles != null && cueFiles.Any())
                {
                    try
                    {
                        ICatalogDataReader theReader = CatalogDataReaderFactory.GetInstance().GetCatalogDataReader(cueFiles.First());
                        result = (short)theReader.Tracks.Max(x => x.TrackNumber);
                    }
                    catch (Exception ex)
                    {
                        Trace.Write("Error Reading Cue: " + ex.ToString());
                    }
                }
                if (!result.HasValue)
                {
                    // See if M3U sheet exists if so read tracks from that and return latest track number
                    var m3uFiles = Directory.GetFiles(directoryName, ("*.m3u"));
                    if (m3uFiles != null && m3uFiles.Any())
                    {
                        try
                        {
                            IPlaylistReader theReader = PlaylistReaderFactory.GetInstance().GetPlaylistReader(m3uFiles.First());
                            result = (short)theReader.GetFiles().Count();
                        }
                        catch (Exception ex)
                        {
                            Trace.Write("Error Reading m3u: " + ex.ToString());
                        }
                    }
                }
            }
            // Try to parse from TrackNumber
            if (!result.HasValue)
            {
                result = ID3TagsHelper.ParseTotalTrackNumber(trackNumber);
            }
            return result;
        }

        public static short? DetermineTrackNumber(string filename)
        {
            filename = filename.Replace("(", "");
            filename = filename.Replace("[", "");
            var part = filename.Substring(0, 2);
            part = part.Replace(".", "");
            part = part.Replace("-", "");
            part = part.Replace(" ", "");
            return SafeParser.ToNumber<short?>(part);
        }

        public static int? ParseDiscNumber(string input)
        {
            var discNumber = SafeParser.ToNumber<int?>(input);
            if (!discNumber.HasValue && !string.IsNullOrEmpty(input))
            {
                input = input.ToUpper().Replace("A", "1");
                input = input.ToUpper().Replace("B", "2");
                input = input.ToUpper().Replace("C", "3");
                input = input.ToUpper().Replace("D", "4");
                input = input.ToUpper().Replace("E", "5");
                discNumber = SafeParser.ToNumber<int?>(input);
                if (!discNumber.HasValue && input.Contains("/"))
                {
                    discNumber = SafeParser.ToNumber<int?>(input.Split("/")[0]);
                }
                if (!discNumber.HasValue && input.Contains("\\"))
                {
                    discNumber = SafeParser.ToNumber<int?>(input.Split("\\")[0]);
                }
                if (!discNumber.HasValue && input.Contains(":"))
                {
                    discNumber = SafeParser.ToNumber<int?>(input.Split(":")[0]);
                }
                if (!discNumber.HasValue && input.Contains(","))
                {
                    discNumber = SafeParser.ToNumber<int?>(input.Split(",")[0]);
                }
                if (!discNumber.HasValue && input.Contains("|"))
                {
                    discNumber = SafeParser.ToNumber<int?>(input.Split("|")[0]);
                }
            }
            return discNumber;
        }

        public static short? ParseTotalTrackNumber(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            var trackparts = input.Split('/');
            if (trackparts.Length < 2)
            {
                return null;
            }
            var r = trackparts[1];
            r = r.ToUpper().Replace("A", "");
            r = r.ToUpper().Replace("B", "");
            r = r.ToUpper().Replace("C", "");
            r = r.ToUpper().Replace("D", "");
            r = r.ToUpper().Replace("E", "");
            return SafeParser.ToNumber<short?>(r) ?? 0;
        }

        public static short? ParseTrackNumber(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            var trackparts = input.Split('/');
            var r = trackparts[0];
            r = r.ToUpper().Replace("A", "");
            r = r.ToUpper().Replace("B", "");
            r = r.ToUpper().Replace("C", "");
            r = r.ToUpper().Replace("D", "");
            r = r.ToUpper().Replace("E", "");
            return SafeParser.ToNumber<short?>(r);
        }

        public static short? ParseYear(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            var date = SafeParser.ToDateTime(input);
            short? year = (short?)date?.Year ?? SafeParser.ToNumber<short?>(input);
            return year > 2200 || year < 1900 ? null : year;
        }

        public OperationResult<AudioMetaData> MetaDataForFile(string fileName, bool returnEvenIfInvalid = false)
        {
            var r = new OperationResult<AudioMetaData>();
            var result = this.MetaDataForFileFromIdSharp(fileName);
            if (result.IsSuccess || returnEvenIfInvalid)
            {
                return result;
            }
            if (result.Messages != null && result.Messages.Any())
            {
                foreach (var m in result.Messages)
                {
                    r.AddMessage(m);
                }
            }
            result = this.MetaDataForFileFromATL(fileName);
            if (result.IsSuccess)
            {
                return result;
            }
            if (result.Messages != null && result.Messages.Any())
            {
                foreach (var m in result.Messages)
                {
                    r.AddMessage(m);
                }
            }
            return r;
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
                if (!metaData.IsValid)
                {
                    this.Logger.LogWarning($"Invalid MetaData `{ metaData }` to save to file [{ filename }]");
                    return false;
                }
                ID3v1Tag.RemoveTag(filename);

                var trackNumber = metaData.TrackNumber ?? 1;
                var totalTrackNumber = metaData.TotalTrackNumbers ?? trackNumber;

                var disc = metaData.Disk ?? 1;
                var discCount = metaData.TotalDiscCount ?? disc;

                IID3v2Tag id3v2 = new ID3v2Tag(filename)
                {
                    Artist = metaData.Artist,
                    Album = metaData.Release,
                    Title = metaData.Title,
                    Year = metaData.Year.Value.ToString(),
                    Genre = metaData.Genres == null || !metaData.Genres.Any() ? null : string.Join("/", metaData.Genres),
                    TrackNumber = totalTrackNumber < 99 ? $"{trackNumber.ToString("00")}/{totalTrackNumber.ToString("00")}" : $"{trackNumber.ToString()}/{totalTrackNumber.ToString()}",
                    DiscNumber = discCount < 99 ? $"{disc.ToString("00")}/{discCount.ToString("00")}" : $"{disc.ToString()}/{discCount.ToString()}"
                };
                if (metaData.TrackArtists.Any())
                {
                    id3v2.OriginalArtist = string.Join("/", metaData.TrackArtists);
                }
                if (this.Configuration.Processing.DoClearComments)
                {
                    if (id3v2.CommentsList.Any())
                    {
                        for (var i = 0; i < id3v2.CommentsList.Count; i++)
                        {
                            id3v2.CommentsList[i].Description = null;
                            id3v2.CommentsList[i].Value = null;
                        }
                    }
                }
                id3v2.Save(filename);

                //// Delete first embedded picture (let's say it exists)
                //theTrack.EmbeddedPictures.RemoveAt(0);

                //// Add 'CD' embedded picture
                //PictureInfo newPicture = new PictureInfo(Commons.ImageFormat.Gif, PictureInfo.PIC_TYPE.CD);
                //newPicture.PictureData = System.IO.File.ReadAllBytes("E:/temp/_Images/pic1.gif");
                //theTrack.EmbeddedPictures.Add(newPicture);

                //// Save modifications on the disc
                //theTrack.Save();

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

        private OperationResult<AudioMetaData> MetaDataForFileFromATL(string fileName)
        {
            var sw = new Stopwatch();
            sw.Start();
            AudioMetaData result = new AudioMetaData();
            var isSuccess = false;
            try
            {
                result.Filename = fileName;
                var theTrack = new ATL.Track(fileName);
                result.Release = theTrack.Album;
                result.Artist = theTrack.AlbumArtist ?? theTrack.Artist;
                result.ArtistRaw = theTrack.AlbumArtist ?? theTrack.Artist;
                result.Genres = theTrack.Genre?.Split(new char[] { ',', '\\' });
                result.TrackArtist = theTrack.OriginalArtist ?? theTrack.Artist ?? theTrack.AlbumArtist;
                result.TrackArtistRaw = theTrack.OriginalArtist ?? theTrack.Artist ?? theTrack.AlbumArtist;
                result.AudioBitrate = (int?)theTrack.Bitrate;
                result.AudioSampleRate = (int)theTrack.Bitrate;
                result.Disk = theTrack.DiscNumber;
                if (theTrack.AdditionalFields.ContainsKey("TSST"))
                {
                    result.DiskSubTitle = theTrack.AdditionalFields["TSST"];
                }
                result.Images = theTrack.EmbeddedPictures?.Select(x => new AudioMetaDataImage
                {
                    Data = x.PictureData,
                    Description = x.Description,
                    MimeType = "image/jpg",
                    Type = x.PicType == PictureInfo.PIC_TYPE.Front || x.PicType == PictureInfo.PIC_TYPE.Generic ? AudioMetaDataImageType.FrontCover : AudioMetaDataImageType.Other
                }).ToArray();
                result.Time = theTrack.DurationMs > 0 ? ((decimal?)theTrack.DurationMs).ToTimeSpan() : null;
                result.Title = theTrack.Title.ToTitleCase(false);
                result.TrackNumber = (short)theTrack.TrackNumber;
                result.Year = theTrack.Year;
                isSuccess = result.IsValid;
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

        private OperationResult<AudioMetaData> MetaDataForFileFromIdSharp(string fileName)
        {
            var sw = new Stopwatch();
            sw.Start();
            AudioMetaData result = new AudioMetaData();
            string message = null;
            var isSuccess = false;
            try
            {
                result.Filename = fileName;
                IAudioFile audioFile = AudioFile.Create(fileName, true);
                if (ID3v2Tag.DoesTagExist(fileName))
                {
                    IID3v2Tag id3v2 = new ID3v2Tag(fileName);
                    result.Artist = id3v2.AlbumArtist ?? id3v2.Artist;
                    result.ArtistRaw = id3v2.AlbumArtist ?? id3v2.Artist;
                    result.AudioBitrate = (int?)audioFile.Bitrate;
                    result.AudioChannels = audioFile.Channels;
                    result.AudioSampleRate = (int)audioFile.Bitrate;
                    result.Comments = id3v2.CommentsList != null ? string.Join("|", id3v2.CommentsList?.Select(x => x.Value)) : null;
                    result.Disk = ID3TagsHelper.ParseDiscNumber(id3v2.DiscNumber);
                    result.DiskSubTitle = id3v2.SetSubtitle;
                    result.Genres = id3v2.Genre?.Split(new char[] { ',', '\\', ';', '|' });
                    result.Release = id3v2.Album;
                    result.TrackArtist = id3v2.OriginalArtist ?? id3v2.Artist ?? id3v2.AlbumArtist;
                    result.TrackArtistRaw = id3v2.OriginalArtist ?? id3v2.Artist ?? id3v2.AlbumArtist;
                    result.Images = id3v2.PictureList?.Select(x => new AudioMetaDataImage
                    {
                        Data = x.PictureData,
                        Description = x.Description,
                        MimeType = x.MimeType,
                        Type = (AudioMetaDataImageType)x.PictureType
                    }).ToArray();
                    result.Time = audioFile.TotalSeconds > 0 ? ((decimal?)audioFile.TotalSeconds).ToTimeSpan() : null;
                    result.Title = id3v2.Title.ToTitleCase(false);
                    result.TrackNumber = ID3TagsHelper.ParseTrackNumber(id3v2.TrackNumber);
                    result.TotalTrackNumbers = ID3TagsHelper.ParseTotalTrackNumber(id3v2.TrackNumber);
                    var year = id3v2.Year ?? id3v2.RecordingTimestamp ?? id3v2.ReleaseTimestamp ?? id3v2.OriginalReleaseTimestamp;
                    result.Year = ID3TagsHelper.ParseYear(year);
                    isSuccess = result.IsValid;
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
                        result.Time = audioFile.TotalSeconds > 0 ? ((decimal?)audioFile.TotalSeconds).ToTimeSpan() : null;
                        result.Title = id3v1.Title.ToTitleCase(false);
                        result.TrackNumber = SafeParser.ToNumber<short?>(id3v1.TrackNumber);
                        var date = SafeParser.ToDateTime(id3v1.Year);
                        result.Year = date?.Year ?? SafeParser.ToNumber<int?>(id3v1.Year);
                        isSuccess = result.IsValid;
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                this.Logger.LogError(ex, "MetaDataForFileFromTagLib Filename [" + fileName + "] Error [" + ex.Serialize() + "]");
            }
            sw.Stop();
            return new OperationResult<AudioMetaData>(message)
            {
                IsSuccess = isSuccess,
                OperationTime = sw.ElapsedMilliseconds,
                Data = result
            };
        }
    }
}