using Roadie.Library.Configuration;
using Roadie.Library.Data;
using Roadie.Library.Extensions;
using Roadie.Library.MetaData.Audio;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Roadie.Library.Utility
{
    /// <summary>
    ///     Helper to determine paths for file storage
    /// </summary>
    public static class FolderPathHelper
    {
        /// <summary>
        ///     Full path to Artist folder
        /// </summary>
        /// <param name="artistSortName">Sort name of Artist to use for folder name</param>
        public static string ArtistPath(IRoadieSettings configuration, string artistSortName)
        {
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(artistSortName),"Invalid Artist Sort Name");

            var artistFolder = artistSortName.ToTitleCase(false);
            var directoryInfo = new DirectoryInfo(Path.Combine(configuration.LibraryFolder, artistFolder.ToFolderNameFriendly()));
            return directoryInfo.FullName;
        }

        public static void DeleteEmptyDirs(string dir, bool deleteDirIfEmpty = true)
        {
            if (string.IsNullOrEmpty(dir))
            {
                throw new ArgumentException("Starting directory is a null reference or an empty string", "dir");
            }
            try
            {
                foreach (var d in Directory.EnumerateDirectories(dir)) DeleteEmptyDirs(d);
                var entries = Directory.EnumerateFileSystemEntries(dir);
                if (!entries.Any() && deleteDirIfEmpty)
                {
                    try
                    {
                        Directory.Delete(dir);
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                    catch (DirectoryNotFoundException)
                    {
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
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
                                Trace.WriteLine(string.Format("Deleting Empty Folder [{0}]", folder.FullName), "Debug");
                            }
                        }
                    }
                    catch (DirectoryNotFoundException)
                    {
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            catch(DirectoryNotFoundException)
            {
            }
            catch (Exception)
            {
                throw;
            }
            return true;
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
        ///     Full path to Release folder using given full Artist folder
        /// </summary>
        /// <param name="artistFolder">Full path to Artist folder</param>
        /// <param name="releaseTitle">Title of Release</param>
        /// <param name="releaseDate">Date of Release</param>
        public static string ReleasePath(string artistFolder, string releaseTitle, DateTime releaseDate)
        {
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(artistFolder), "Invalid Artist Folder");
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(releaseTitle), "Invalid Release Title");
            SimpleContract.Requires<ArgumentException>(releaseDate != DateTime.MinValue, "Invalid Release Date");

            var directoryInfo = new DirectoryInfo(Path.Combine(artistFolder, string.Format("{1}{0}", releaseTitle.ToTitleCase(false).ToFolderNameFriendly(), string.Format("[{0}] ", releaseDate.ToString("yyyy")))));
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
                foreach (var kp in trackPathReplace)
                    fileNameFromTitle = fileNameFromTitle.Replace(kp.Key, kp.Value);

            return string.Format("{0}{1} {2}.{3}", disc, track, fileNameFromTitle, fileExtension.ToLower());
        }

        /// <summary>
        ///     Returns the Full path (FQDN) for given Track details
        /// </summary>
        /// <param name="metaData">Populated Track MetaData</param>
        /// <param name="artistFolder">Optional ArtistFolder default is to get from MetaData artist</param>
        public static string TrackFullPath(IRoadieSettings configuration, AudioMetaData metaData, string artistFolder = null, string releaseFolder = null)
        {
            return TrackFullPath(configuration, metaData.Artist, metaData.Release,
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
        public static string TrackFullPath(IRoadieSettings configuration, Artist artist, Release release, Track track) => TrackFullPath(configuration, artist.SortNameValue, release.Title, release.ReleaseDate.Value, track.Title, track.TrackNumber);

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
        public static string TrackFullPath(IRoadieSettings configuration, string artistSortName, string releaseTitle,
            DateTime releaseDate, string trackTitle, short trackNumber, int? discNumber = null, int? totalTrackNumber = null, string fileExtension = "mp3",
            string artistFolder = null, string releaseFolder = null)
        {
            artistFolder = artistFolder ?? ArtistPath(configuration, artistSortName);
            releaseFolder = releaseFolder ?? ReleasePath(artistFolder, releaseTitle, releaseDate);
            var trackFileName = TrackFileName(configuration, trackTitle, trackNumber, discNumber, totalTrackNumber, fileExtension);

            var result = Path.Combine(artistFolder, releaseFolder, trackFileName);
            var resultInfo = new DirectoryInfo(result);
            Trace.WriteLine(string.Format(
                "TrackPath [{0}] For ArtistName [{1}], ReleaseTitle [{2}], ReleaseDate [{3}], ReleaseYear [{4}], TrackNumber [{5}]",
                resultInfo.FullName, artistSortName, releaseTitle, releaseDate.ToString("s"),
                releaseDate.ToString("yyyy"), trackNumber));
            return resultInfo.FullName;
        }

        /// <summary>
        ///     Returns the Directory for a Track (just directory not Track FileName)
        /// </summary>
        /// <param name="metaData">Populated Track MetaData</param>
        /// <param name="destinationFolder">Optional Root folder defaults to Library Folder from Settings</param>
        /// <param name="artistFolder">Optional ArtistFolder default is to get from MetaData artist</param>
        /// ///
        public static string TrackPath(IRoadieSettings configuration, AudioMetaData metaData,
            string destinationFolder = null, string artistFolder = null)
        {
            var fileInfo = new FileInfo(TrackFullPath(configuration, metaData, destinationFolder, artistFolder));
            return fileInfo.Directory.Name;
        }

        /// <summary>
        ///     Returns the Directory for a Track (just directory not Track FileName)
        /// </summary>
        /// <param name="artist">Artist For release</param>
        /// <param name="release">Release</param>
        /// <param name="track">Track</param>
        public static string TrackPath(IRoadieSettings configuration, Artist artist, Release release, Track track)
        {
            var fileInfo = new FileInfo(TrackFullPath(configuration, artist.SortNameValue, release.Title, release.ReleaseDate.Value, track.Title, track.TrackNumber));
            return fileInfo.Directory.Name;
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
        public static string TrackPath(IRoadieSettings configuration, string artistSortName, string releaseTitle,
            DateTime releaseDate, string trackTitle, short trackNumber, int? discNumber = null, int? totalTrackNumber = null)
        {
            var fileInfo = new FileInfo(TrackFullPath(configuration, artistSortName, releaseTitle, releaseDate, trackTitle, trackNumber, discNumber, totalTrackNumber));
            return fileInfo.Directory.Name;
        }
    }
}