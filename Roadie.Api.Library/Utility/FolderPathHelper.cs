using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Extensions;
using Roadie.Library.MetaData.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Roadie.Library.Utility
{
    /// <summary>
    ///     Helper to determine paths for file storage
    /// </summary>
    public static class FolderPathHelper
    {
        public static int MaximumLibraryFolderNameLength = 44;
        public static int MaximumArtistFolderNameLength = 100;
        public static int MaximumReleaseFolderNameLength = 100;
        public static int MaximumLabelFolderNameLength = 100;
        public static int MaximumTrackFileNameLength = 500;

        public static IEnumerable<string> FolderSpaceReplacements = new List<string> { ".", "~", "_", "=", "-" };

        /// <summary>
        ///     Full path to Artist folder
        /// </summary>
        /// <param name="artistSortName">Sort name of Artist to use for folder name</param>
        public static string ArtistPath(IRoadieSettings configuration, int artistId, string artistSortName)
        {
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(artistSortName), "Invalid Artist Sort Name");
            SimpleContract.Requires<ArgumentException>(configuration.LibraryFolder.Length < MaximumLibraryFolderNameLength, $"Library Folder maximum length is [{ MaximumLibraryFolderNameLength }]");

            var asn = new StringBuilder(artistSortName);
            foreach (var stringReplacement in FolderSpaceReplacements)
            {
                if (!asn.Equals(stringReplacement))
                {
                    asn.Replace(stringReplacement, " ");
                }
            }
            var artistFolder = asn.ToString().ToAlphanumericName(false, false).ToFolderNameFriendly().ToTitleCase(false);
            if (string.IsNullOrEmpty(artistFolder))
            {
                throw new Exception($"ArtistFolder [{ artistFolder }] is invalid. ArtistId [{ artistId }], ArtistSortName [{ artistSortName }].");
            }
            var afUpper = artistFolder.ToUpper();
            var fnSubPart1 = afUpper.ToUpper().ToCharArray().Take(1).First();
            if (!char.IsLetterOrDigit(fnSubPart1))
            {
                fnSubPart1 = '#';
            }
            else if (char.IsNumber(fnSubPart1))
            {
                fnSubPart1 = '0';
            }
            var fnSubPart2 = afUpper.Length > 2 ? afUpper.Substring(0, 2) : afUpper;
            if (fnSubPart2.EndsWith(" "))
            {
                var pos = 1;
                while (fnSubPart2.EndsWith(" "))
                {
                    pos++;
                    fnSubPart2 = fnSubPart2.Substring(0, 1) + afUpper.Substring(pos, 1);
                }
            }
            var fnSubPart = Path.Combine(fnSubPart1.ToString(), fnSubPart2);
            var fnIdPart = $" [{ artistId }]";
            var maxFnLength = (MaximumArtistFolderNameLength - (fnSubPart.Length + fnIdPart.Length)) - 2;
            if (artistFolder.Length > maxFnLength)
            {
                artistFolder = artistFolder.Substring(0, maxFnLength);
            }
            artistFolder = Path.Combine(fnSubPart, $"{ artistFolder }{ fnIdPart }");
            var directoryInfo = new DirectoryInfo(Path.Combine(configuration.LibraryFolder, artistFolder));
            return directoryInfo.FullName;
        }


        public static string LabelPath(IRoadieSettings configuration, string labelSortName)
        {
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(labelSortName), "Invalid Label Sort Name");
            SimpleContract.Requires<ArgumentException>(configuration.LibraryFolder.Length < MaximumLibraryFolderNameLength, $"Library Folder maximum length is [{ MaximumLibraryFolderNameLength }]");

            var lsn = new StringBuilder(labelSortName);
            foreach (var stringReplacement in FolderSpaceReplacements)
            {
                if (!lsn.Equals(stringReplacement))
                {
                    lsn.Replace(stringReplacement, " ");
                }
            }
            var labelFolder = lsn.ToString().ToAlphanumericName(false, false).ToFolderNameFriendly().ToTitleCase(false);
            if (string.IsNullOrEmpty(labelFolder))
            {
                throw new Exception($"LabelFolder [{ labelFolder }] is invalid. LabelSortName [{ labelSortName }].");
            }
            var lfUpper = labelFolder.ToUpper();
            var fnSubPart1 = lfUpper.ToUpper().ToCharArray().Take(1).First();
            if (!char.IsLetterOrDigit(fnSubPart1))
            {
                fnSubPart1 = '#';
            }
            else if (char.IsNumber(fnSubPart1))
            {
                fnSubPart1 = '0';
            }
            var fnSubPart2 = lfUpper.Length > 2 ? lfUpper.Substring(0, 2) : lfUpper;
            if (fnSubPart2.EndsWith(" "))
            {
                var pos = 1;
                while (fnSubPart2.EndsWith(" "))
                {
                    pos++;
                    fnSubPart2 = fnSubPart2.Substring(0, 1) + lfUpper.Substring(pos, 1);
                }
            }
            var fnSubPart = Path.Combine(fnSubPart1.ToString(), fnSubPart2);
            var directoryInfo = new DirectoryInfo(Path.Combine(configuration.LabelImageFolder, fnSubPart));
            return directoryInfo.FullName;
        }

        [Obsolete("This is only here for migration will be removed in future release.")]
        public static string ArtistPathOld(IRoadieSettings configuration, string artistSortName)
        {
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(artistSortName),"Invalid Artist Sort Name");

            var artistFolder = artistSortName.ToTitleCase(false);
            var directoryInfo = new DirectoryInfo(Path.Combine(configuration.LibraryFolder, artistFolder.ToFolderNameFriendly()));
            return directoryInfo.FullName;
        }


        /// <summary>
        ///     Full path to Release folder using given full Artist folder
        /// </summary>
        /// <param name="artistFolder">Full path to Artist folder</param>
        /// <param name="releaseTitle">Title of Release</param>
        /// <param name="releaseDate">Date of Release</param>
        public static string ReleasePath(string artistFolder, string releaseTitle, DateTime releaseDate)
        {
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(artistFolder), "Invalid Artist Folder");
            SimpleContract.Requires<ArgumentException>(artistFolder.Length < MaximumArtistFolderNameLength, $"Artist Folder is longer than maximum allowed [{ MaximumArtistFolderNameLength }]");

            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(releaseTitle), "Invalid Release Title");
            SimpleContract.Requires<ArgumentException>(releaseDate != DateTime.MinValue, "Invalid Release Date");

            var rt = new StringBuilder(releaseTitle);
            foreach (var stringReplacement in FolderSpaceReplacements)
            {
                if(!rt.Equals(stringReplacement))
                {
                    rt.Replace(stringReplacement, " ");
                }
            }
            var releasePathTitle = rt.ToString().ToAlphanumericName(false, false).ToFolderNameFriendly().ToTitleCase(false);
            if(string.IsNullOrEmpty(releasePathTitle))
            {
                throw new Exception($"ReleaseTitle [{ releaseTitle }] is invalid. ArtistFolder [{ artistFolder }].");
            }
            var maxFnLength = MaximumReleaseFolderNameLength - 7;
            if (releasePathTitle.Length > maxFnLength)
            {
                releasePathTitle = releasePathTitle.Substring(0, maxFnLength);
            }
            var releasePath = $"[{ releaseDate.ToString("yyyy")}] {releasePathTitle}";
            var directoryInfo = new DirectoryInfo(Path.Combine(artistFolder, releasePath));
            return directoryInfo.FullName;
        }

        [Obsolete("This is only here for migration will be removed in future release.")]
        public static string ReleasePathOld(string artistFolder, string releaseTitle, DateTime releaseDate)
        {
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(artistFolder), "Invalid Artist Folder");
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(releaseTitle), "Invalid Release Title");
            SimpleContract.Requires<ArgumentException>(releaseDate != DateTime.MinValue, "Invalid Release Date");

            var directoryInfo = new DirectoryInfo(Path.Combine(artistFolder, string.Format("{1}{0}", releaseTitle.ToTitleCase(false).ToFolderNameFriendly(), string.Format("[{0}] ", releaseDate.ToString("yyyy")))));
            return directoryInfo.FullName;
        }

        /// <summary>
        ///     Delete any empty folders in the given folder
        /// </summary>
        /// <param name="processingFolder"></param>
        /// <returns></returns>
        public static bool DeleteEmptyFolders(DirectoryInfo processingFolder)
        {
            if (processingFolder == null || !processingFolder.Exists)
            {
                return true;
            }
            var result = false;
            try
            {
                foreach (var folder in processingFolder.GetDirectories("*.*", SearchOption.AllDirectories))
                {
                    try
                    {
                        if (folder.Exists)
                        {
                            if (!folder.GetFiles("*.*", SearchOption.AllDirectories).Any())
                            {
                                folder.Delete(true);
                                Trace.WriteLine($"Deleting Empty Folder [{folder.FullName}]", "Debug");
                                result = true;
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        result = false;
                        Trace.WriteLine($"UnauthorizedAccessException Deleting Empty Folder [{folder.FullName}]", "Debug");
                    }
                    catch (DirectoryNotFoundException)
                    {
                        result = false;
                        Trace.WriteLine($"DirectoryNotFoundException Deleting Empty Folder [{folder.FullName}]", "Debug");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                result = false;
                Trace.WriteLine($"UnauthorizedAccessException Deleting Empty Folder [{processingFolder.FullName}]", "Debug");
            }
            catch (DirectoryNotFoundException)
            {
                result = false;
                Trace.WriteLine($"DirectoryNotFoundException Deleting Empty Folder [{processingFolder.FullName}]", "Debug");
            }
            return result;
        }

        /// <summary>
        ///     For given artist delete any empty folders
        /// </summary>
        /// <param name="artist">Populated Artist database record</param>
        public static bool DeleteEmptyFoldersForArtist(IRoadieSettings configuration, Artist artist)
        {
            SimpleContract.Requires<ArgumentException>(artist != null, "Invalid Artist");
            return DeleteEmptyFolders(new DirectoryInfo(artist.ArtistFileFolder(configuration)));
        }

        /// <summary>
        ///     For a given Track database record determine the path using the given destination folder
        /// </summary>
        /// <param name="track">Populate track database record</param>
        /// <param name="destinationFolder">Optional Root folder defaults to Library Folder from Settings</param>
        public static string PathForTrack(IRoadieSettings configuration, Track track)
        {
            if (string.IsNullOrEmpty(track.FilePath) || string.IsNullOrEmpty(track.FileName))
            {
                return null;
            }
            var directoryInfo = new DirectoryInfo(Path.Combine(configuration.LibraryFolder, track.FilePath, track.FileName));
            return directoryInfo.FullName;
        }

        /// <summary>
        ///     For a given Track database record determine the full path to the thumbnail using the given destination folder
        /// </summary>
        /// <param name="track">Populate track database record</param>
        /// <param name="destinationFolder">Optional Root folder defaults to Library Folder from Settings</param>
        public static string PathForTrackThumbnail(IRoadieSettings configuration, Track track, string destinationFolder = null)
        {
            destinationFolder = destinationFolder ?? configuration.LibraryFolder;
            if (string.IsNullOrEmpty(track.FilePath) || string.IsNullOrEmpty(track.FileName))
            {
                return null;
            }
            var fileName = Path.ChangeExtension(track.FileName, ".jpg");
            var directoryInfo = new DirectoryInfo(Path.Combine(destinationFolder, track.FilePath, fileName));
            return directoryInfo.FullName;
        }

        /// <summary>
        ///     Returns the FileName for given Track details, this is not the Full Path (FQDN) only the FileName
        /// </summary>
        /// <param name="metaData">Populated Track MetaData</param>
        public static string TrackFileName(IRoadieSettings configuration, AudioMetaData metaData)
        {
            var fileInfo = new FileInfo(metaData.Filename);
            return TrackFileName(configuration, metaData.Release, metaData.TrackNumber ?? 0, metaData.Disc, metaData.TotalTrackNumbers, fileInfo.Extension.ToLower());
        }

        /// <summary>
        ///     Returns the FileName for given Track details, this is not the Full Path (FQDN) only the FileName
        /// </summary>
        /// <param name="trackTitle">Title of the Track</param>
        /// <param name="trackNumber">Track Number</param>
        /// <param name="discNumber">Optional disc number defaults to 0</param>
        /// <param name="totalTrackNumber">Optional Total Tracks defaults to TrackNumber</param>
        /// <param name="fileExtension">Optional File Extension defaults to mp3</param>
        public static string TrackFileName(IRoadieSettings configuration, string trackTitle, short trackNumber, int? discNumber = null, int? totalTrackNumber = null, string fileExtension = "mp3")
        {
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(trackTitle), "Invalid Track Title");
            SimpleContract.Requires<ArgumentException>(trackNumber > 0, "Invalid Track Number");
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(fileExtension), "Invalid File Extension");

            // If the total number of tracks is more than 99 or the track number itself is more than 99 then 3 pad else 2 pad
            var track = (totalTrackNumber ?? trackNumber) > 99 || trackNumber > 99
                ? trackNumber.ToString("D3")
                : trackNumber.ToString("D2");
            // Put an "m" for media on the TPOS greater than 1 so the directory sorts proper
            var dn = discNumber ?? 0;
            var disc = dn > 1 ? string.Format("m{0} ", dn.ToString("D3")) : string.Empty;

            // Get new name for file
            var fileNameFromTitle = trackTitle.ToTitleCase(false).ToFileNameFriendly();
            if (fileNameFromTitle.StartsWith(track))
                fileNameFromTitle = fileNameFromTitle
                    .RemoveStartsWith(string.Format("{0} -", track))
                    .RemoveStartsWith(string.Format("{0} ", track))
                    .RemoveStartsWith(string.Format("{0}.", track))
                    .RemoveStartsWith(string.Format("{0}-", track))
                    .ToTitleCase(false);
            var trackPathReplace = configuration.TrackPathReplace;
            if (trackPathReplace != null)
            {
                foreach (var kp in trackPathReplace)
                {
                    fileNameFromTitle = fileNameFromTitle.Replace(kp.Key, kp.Value);
                }
            }
            return string.Format("{0}{1} {2}.{3}", disc, track, fileNameFromTitle, fileExtension.ToLower());
        }

        /// <summary>
        ///     Returns the Full path (FQDN) for given Track details
        /// </summary>
        /// <param name="metaData">Populated Track MetaData</param>
        /// <param name="artistFolder">Optional ArtistFolder default is to get from MetaData artist</param>
        public static string TrackFullPath(IRoadieSettings configuration, AudioMetaData metaData, string artistFolder = null, string releaseFolder = null)
        {
            return TrackFullPath(configuration, 0, metaData.Artist, metaData.Release,
                SafeParser.ToDateTime(metaData.Year).Value,
                metaData.Title, metaData.TrackNumber ?? 0, metaData.Disc ?? 0,
                metaData.TotalTrackNumbers ?? 0,
                artistFolder: artistFolder,
                releaseFolder: releaseFolder);
        }

        /// <summary>
        ///     Returns the Full path (FQDN) for given Track details
        /// </summary>
        /// <param name="artist">Artist For release</param>
        /// <param name="release">Release</param>
        /// <param name="track">Track</param>
        /// <param name="destinationFolder">Optional Root folder defaults to Library Folder from Settings</param>
        /// <returns></returns>
        public static string TrackFullPath(IRoadieSettings configuration, Artist artist, Release release, Track track) => TrackFullPath(configuration, artist.Id, artist.SortNameValue, release.SortTitleValue, release.ReleaseDate.Value, track.Title, track.TrackNumber);

        /// <summary>
        ///     Return the full path (FQDN) for given Track details
        /// </summary>
        /// <param name="artistSortName">Sort name of Artist to use for folder name</param>
        /// <param name="releaseTitle">Title of Release</param>
        /// <param name="releaseDate">Date of Release</param>
        /// <param name="trackNumber">Track Number</param>
        /// <param name="destinationFolder">Optional Root folder defaults to Library Folder from Settings</param>
        /// <param name="discNumber">Optional disc number defaults to 0</param>
        /// <param name="totalTrackNumber">Optional Total Tracks defaults to TrackNumber</param>
        /// <param name="fileExtension">Optional File Extension defaults to mp3</param>
        public static string TrackFullPath(IRoadieSettings configuration, int artistId, string artistSortName, string releaseTitle,
            DateTime releaseDate, string trackTitle, short trackNumber, int? discNumber = null, int? totalTrackNumber = null, string fileExtension = "mp3",
            string artistFolder = null, string releaseFolder = null)
        {
            artistFolder ??= ArtistPath(configuration, artistId, artistSortName);
            releaseFolder ??= ReleasePath(artistFolder, releaseTitle, releaseDate);
            var trackFileName = TrackFileName(configuration, trackTitle, trackNumber, discNumber, totalTrackNumber, fileExtension);

            var result = Path.Combine(artistFolder, releaseFolder, trackFileName);
            var resultInfo = new DirectoryInfo(result);
            return resultInfo.FullName;
        }

        /// <summary>
        ///     Returns the Directory for a Track (just directory not Track FileName)
        /// </summary>
        /// <param name="metaData">Populated Track MetaData</param>
        /// <param name="destinationFolder">Optional Root folder defaults to Library Folder from Settings</param>
        /// <param name="artistFolder">Optional ArtistFolder default is to get from MetaData artist</param>
        /// ///
        public static string TrackPath(IRoadieSettings configuration, AudioMetaData metaData, string destinationFolder = null, string artistFolder = null)
        {
            var fileInfo = new FileInfo(TrackFullPath(configuration, metaData, destinationFolder, artistFolder));
            var tf = fileInfo.Directory.Parent.FullName.Replace(new DirectoryInfo(configuration.LibraryFolder).FullName, "");
            if (tf.StartsWith(Path.DirectorySeparatorChar))
            {
                tf = tf.RemoveFirst(Path.DirectorySeparatorChar.ToString());
            }
            return Path.Combine(tf, fileInfo.Directory.Name);
        }

        /// <summary>
        ///     Returns the Directory for a Track (just directory not Track FileName)
        /// </summary>
        /// <param name="artist">Artist For release</param>
        /// <param name="release">Release</param>
        /// <param name="track">Track</param>
        public static string TrackPath(IRoadieSettings configuration, Artist artist, Release release, Track track)
        {
            var fileInfo = new FileInfo(TrackFullPath(configuration, artist.Id, artist.SortNameValue, release.SortTitleValue, release.ReleaseDate.Value, track.Title, track.TrackNumber));
            var tf = fileInfo.Directory.Parent.FullName.Replace(new DirectoryInfo(configuration.LibraryFolder).FullName, "");
            if (tf.StartsWith(Path.DirectorySeparatorChar))
            {
                tf = tf.RemoveFirst(Path.DirectorySeparatorChar.ToString());
            }
            var result = Path.Combine(tf, fileInfo.Directory.Name);
            return result;
        }

        /// <summary>
        ///     Returns the Directory for a Track (just directory not Track FileName)
        /// </summary>
        /// <param name="artistSortName">Sort name of Artist to use for folder name</param>
        /// <param name="releaseTitle">Title of Release</param>
        /// <param name="releaseDate">Date of Release</param>
        /// <param name="trackNumber">Track Number</param>
        /// <param name="destinationFolder">Optional Root folder defaults to Library Folder from Settings</param>
        /// <param name="discNumber">Optional disc number defaults to 0</param>
        /// <param name="totalTrackNumber">Optional Total Tracks defaults to TrackNumber</param>
        public static string TrackPath(IRoadieSettings configuration, int artistId, string artistSortName, string releaseTitle,
            DateTime releaseDate, string trackTitle, short trackNumber, int? discNumber = null, int? totalTrackNumber = null)
        {
            var fileInfo = new FileInfo(TrackFullPath(configuration, artistId, artistSortName, releaseTitle, releaseDate, trackTitle, trackNumber, discNumber, totalTrackNumber));
            var tf = fileInfo.Directory.Parent.FullName.Replace(new DirectoryInfo(configuration.LibraryFolder).FullName, "");
            if (tf.StartsWith(Path.DirectorySeparatorChar))
            {
                tf = tf.RemoveFirst(Path.DirectorySeparatorChar.ToString());
            }
            var result = Path.Combine(tf, fileInfo.Directory.Name);
            return result;
        }
    }
}