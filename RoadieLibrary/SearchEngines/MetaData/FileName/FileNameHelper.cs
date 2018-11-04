using Roadie.Library.Caching;
using Roadie.Library.Extensions;
using Roadie.Library.Utility;
using Roadie.Library.Logging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Roadie.Library.MetaData.Audio;
using Microsoft.Extensions.Configuration;

namespace Roadie.Library.MetaData.FileName
{
    public class FileNameHelper : MetaDataProviderBase
    {
        public FileNameHelper(IConfiguration configuration, ICacheManager cacheManager, ILogger loggingService) 
            : base(configuration, cacheManager, loggingService)
        { }

        public string CleanString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return input.CleanString(this.Configuration);
        }

        public AudioMetaData MetaDataFromFilename(string rawFilename)
        {
            var filename = CleanString(rawFilename);
            if (IsTalbTyerTalbTrckTit2(filename))
            {
                // GUID~TPE1 - [TYER] - TALB~TRCK. TIT2
                var parts = filename.Split('~');

                var firstParts = parts[1].Split('-');
                var artist = firstParts[0];
                var year = firstParts[1].Replace("[", "").Replace("]", "");
                var Release = firstParts[2];

                var trck = parts[2].Substring(0, 2);
                var title = parts[2].Substring(3, parts[2].Length - 3);

                return new AudioMetaData
                {
                    Artist = CleanString(artist),
                    Year = SafeParser.ToNumber<int?>(CleanString(year)),
                    Release = CleanString(Release),
                    TrackNumber = SafeParser.ToNumber<short?>(CleanString(trck)),
                    Title = CleanString(title)
                };
            }
            else if (IsTpe1TalbTyerTrckTpe1Tit2(filename))
            {
                // GUID~TPE1-TALB-TYER~TRCK-TPE1-TIT2
                var parts = filename.Split('~');

                var firstParts = parts[1].Split('-');
                var secondParts = parts[2].Split('-');

                var artist = firstParts[0];
                var Release = firstParts[1];
                var year = firstParts[2];
                var trck = secondParts[0].Substring(0, 2);
                var title = secondParts[1];

                return new AudioMetaData
                {
                    Artist = CleanString(artist),
                    Year = SafeParser.ToNumber<int?>(CleanString(year)),
                    Release = CleanString(Release),
                    TrackNumber = SafeParser.ToNumber<short?>(CleanString(trck)),
                    Title = CleanString(title)
                };
            }
            else if (IsTpe1TalbTyerTrckTit2(filename))
            {
                // GUID~TPE1-TALB (TYER)~TRCK-TIT2
                var parts = filename.Split('~');

                var firstParts = parts[1].Split('-');
                var secondParts = parts[2].Split('-');

                var artist = firstParts[0];
                var year = firstParts[1].Substring(firstParts[1].Length - 6, 6).Replace("(", "").Replace(")", "");
                var Release = firstParts[1].Substring(0, firstParts[1].Length - 6);
                var trck = secondParts[1];
                var title = secondParts[2];

                return new AudioMetaData
                {
                    Artist = CleanString(artist),
                    Year = SafeParser.ToNumber<int?>(CleanString(year)),
                    Release = CleanString(Release),
                    TrackNumber = SafeParser.ToNumber<short?>(CleanString(trck)),
                    Title = CleanString(title)
                };
            }
            else if (IsTalbTposTpe1TrckTit2(filename))
            {
                // GUID~TALB~TPOS TPE1 - TRCK - TIT2
                var parts = filename.Split('~');
                var Release = parts[1];

                var secondParts = parts[2].Split('-');

                var tpos = secondParts[0].Substring(0, 2);
                var artist = secondParts[0].Substring(2, secondParts[0].Length - 2);
                var trck = secondParts[1];
                var title = secondParts[2];

                return new AudioMetaData
                {
                    Artist = CleanString(artist),
                    Disk = SafeParser.ToNumber<int?>(CleanString(tpos)),
                    Release = CleanString(Release),
                    TrackNumber = SafeParser.ToNumber<short?>(CleanString(trck)),
                    Title = CleanString(title)
                };
            }
            else if (IsTyerTalbTrckTit2(filename))
            {
                // GUID~[TYER] TALB~TRCK - TIT2
                var parts = filename.Split('~');
                var year = parts[1].Split(' ').First().Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "");
                var Release = string.Join(" ", parts[1].Split(' ').Skip(1));

                var secondParts = parts[2];
                if (secondParts.StartsWith("-"))
                {
                    secondParts = secondParts.Substring(1, secondParts.Length - 1);
                }
                var track = secondParts.Split('-').First();
                var title = string.Join(" ", secondParts.Split('-').Skip(1));
                return new AudioMetaData
                {
                    Year = SafeParser.ToNumber<int?>(CleanString(year)),
                    Release = CleanString(Release),
                    TrackNumber = SafeParser.ToNumber<short?>(CleanString(track)),
                    Title = CleanString(title)
                };
            }
            else if (IsTyerTalbTpe1Tit2(filename))
            {
                // GUID~TYER - TALB~TPE1 - TIT2
                var parts = filename.Split('~');
                var secondParts = parts[1].Split('-');

                var year = secondParts[0];
                var Release = secondParts[1];

                var thirdParts = parts[2].Split('-');
                var artist = thirdParts[0];
                var title = thirdParts[1];

                return new AudioMetaData
                {
                    Year = SafeParser.ToNumber<int?>(CleanString(year)),
                    Artist = CleanString(artist),
                    Release = CleanString(Release),
                    Title = CleanString(title)
                };
            }
            else if (IsTpe1TrckTit2(filename))
            {
                // GUID~TPE1~TRCK TIT2
                var parts = filename.Split('~');
                var track = parts[2].Split(' ').First();
                var title = string.Join(" ", parts[2].Split(' ').Skip(1));
                return new AudioMetaData
                {
                    Artist = CleanString(parts[1]),
                    TrackNumber = SafeParser.ToNumber<short?>(CleanString(track)),
                    Title = CleanString(title)
                };
            }
            else if (IsTpe1Tit2(filename))
            {
                var parts = filename.Split('~');
                return new AudioMetaData
                {
                    Artist = CleanString(parts[1]),
                    Title = CleanString(parts[2])
                };
            }

            return new AudioMetaData();
        }

        public AudioMetaData MetaDataFromFileInfo(FileInfo fileInfo)
        {
            var justFilename = CleanString(fileInfo.Name.Replace(fileInfo.Extension, ""));
            if (IsTrckTit2(justFilename))
            {
                var Release = fileInfo.Directory.Name;
                var ReleaseYear = SafeParser.ToYear(Release.Substring(0, 4));
                if (ReleaseYear.HasValue)
                {
                    Release = Release.Substring(5, Release.Length - 5);
                }
                var artist = fileInfo.Directory.Parent.Name;

                var title = justFilename.Substring(2, justFilename.Length - 2);
                var artistYearRelease = CleanString(string.Format("{0} {1} {2}", artist, ReleaseYear, Release));
                if (justFilename.StartsWith(artistYearRelease) || CleanString(justFilename.Replace("The ", "")).StartsWith(CleanString(artistYearRelease.Replace("The ", ""))))
                {
                    title = CleanString(CleanString(justFilename.Replace("The ", "")).Replace(CleanString(artistYearRelease.Replace("The ", "")), ""));
                }
                else
                {
                    var regex = new Regex(@"[0-9]{2}-[\w\s,$/.'-`#&()!]+");
                    if (regex.IsMatch(title))
                    {
                        title = fileInfo.Name.Replace(fileInfo.Extension, "");
                        title = title.Substring(6, title.Length - 6);
                        title = Regex.Replace(title, @"(\B[A-Z]+?(?=[A-Z][^A-Z])|\B[A-Z]+?(?=[^A-Z]))", " $1");
                    }
                }
                var trackNumber = SafeParser.ToNumber<short>(title.Substring(0, 2));
                return new AudioMetaData
                {
                    Artist = artist,
                    Release = Release,
                    Year = ReleaseYear,
                    TrackNumber = trackNumber,
                    Title = CleanString(title.Replace(trackNumber.ToString("D2") + " ", ""))
                };
            }
            return new AudioMetaData();
        }

        public static bool IsValidAudioFileName(string filename)
        {
            var regex = new Regex(@"[a-zA-Z]:\\[\\\w\s,\$\/\.'`#&()!\‐\-]+\[[0-9]{4}\]\s[\[\]\w\s,\$\/\.'`#&()!\‐\-]+[\\CD0-9]*\\[0-9]{2,}\s[\[\]\w\s,\$\/\.'`#&()!\‐\-]+\.(mp3|flac)");
            return regex.IsMatch(filename);
        }

        /// <summary>
        /// TRCK TIT2
        /// </summary>
        public static bool IsTrckTit2(string filename)
        {
            var regex = new Regex(@"[0-9]{2}\s[\w\s,$/.'-`#&()!]+");
            return regex.IsMatch(filename);
        }

        /// <summary>
        /// GUID~TALB~TPOS TPE1 - TRCK - TIT2
        /// </summary>
        public static bool IsTalbTposTpe1TrckTit2(string filename)
        {
            var regex = new Regex(@"[-a-zA-Z0-9]{36}~[\w\s,$/.'`#&()!-]+[\s\w()'&]~[0-9]{2}[-\w\s,$/.'`#&()!]+[-\s~]\s[0-9]{1,2}[-\s~]+[\w\s,$/.'-`#&()!]+");
            return regex.IsMatch(filename);
        }

        /// <summary>
        /// GUID~TPE1 - [TYER] - TALB~TRCK. TIT2
        /// </summary>
        public static bool IsTalbTyerTalbTrckTit2(string filename)
        {
            var regex = new Regex(@"[-a-zA-Z0-9]{36}~[\w\s,$/.'`#&()!]+-\s\[[0-9]{4}]\s-\s[\w\s,$/.'`#&()!]+~[0-9]{2}.[\w\s,$/.'`#&()!]+");
            return regex.IsMatch(filename);
        }

        /// <summary>
        /// GUID~TPE1~TIT2
        /// </summary>
        public static bool IsTpe1Tit2(string filename)
        {
            var regex = new Regex(@"[-a-zA-Z0-9]{36}~[\w\s,$/.'-`#&()!]+~[\w\s,$/.'-`#&()!]+");
            return regex.IsMatch(filename);
        }

        /// <summary>
        /// GUID~TYER - TALB~TPE1 - TIT2
        /// </summary>
        public static bool IsTyerTalbTpe1Tit2(string filename)
        {
            var regex = new Regex(@"[-a-zA-Z0-9]{36}~[0-9]{4}[\w\s,$/.'-`#&()!]+~[\w\s,$/.'-`#&()!]+");
            return regex.IsMatch(filename);
        }

        /// <summary>
        /// GUID~[TYER] TALB~TRCK - TIT2
        /// </summary>
        public static bool IsTyerTalbTrckTit2(string filename)
        {
            var regex = new Regex(@"[-a-zA-Z0-9]{36}~[\[\(]*[0-9]{4}[\]\)]*[\w\s,$/.'-`#&()!]+[~-]*((\\)*(/)*([0-9]{2})*)[\s~-]*[\w\s,$/.'-`#&()!]+");
            return regex.IsMatch(filename);
        }

        /// <summary>
        /// GUID~TPE1-TALB-TYER~TRCK-TPE1-TIT2
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static bool IsTpe1TalbTyerTrckTpe1Tit2(string filename)
        {
            var regex = new Regex(@"[-a-zA-Z0-9]{36}~[\w\s,$/.'`#&()!]+-[\w\s,$/.'`#&()!]+-[0-9]{4}[\]\)]*~[\w\s,$/.'`#&()!]+-[\w\s,$/.'`#&()!]+");
            return regex.IsMatch(filename);
        }

        /// <summary>
        /// GUID~TPE1-TALB (TYER)~TRCK-TIT2
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static bool IsTpe1TalbTyerTrckTit2(string filename)
        {
            var regex = new Regex(@"[-a-zA-Z0-9]{36}~[\w\s,$/.'`#&()!]+-[\w\s,$/.'`#&()!]+~[\w\s,$/.'`#&()!]+-\s[0-9]{2}\s-[\w\s,$/.'`#&()!]+");
            return regex.IsMatch(filename);
        }

        /// <summary>
        /// GUID~TPE1~TRCK TIT2
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static bool IsTpe1TrckTit2(string filename)
        {
            var regex = new Regex(@"[-a-zA-Z0-9]{36}~[-\w\s,$/.'`#&()!]+[ -~][0-9]{2}[\w\s,$/.'-`#&()!]+");
            return regex.IsMatch(filename);
        }
    }
}