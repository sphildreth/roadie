using Roadie.Library.Configuration;
using Roadie.Library.Extensions;
using Roadie.Library.MetaData.Audio;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Roadie.Library.Utility
{
    /// <summary>
    /// Helper to determine paths for file storage
    /// </summary>
    public static class FolderPathHelper
    {
        /// <summary>
        /// Full path to Artist folder using destinationFolder as folder Root
        /// </summary>
        /// <param name="artistSortName">Sort name of Artist to use for folder name</param>
        /// <param name="destinationFolder">Optional Root folder defaults to Library Folder from Settings</param>
        public static string ArtistPath(IRoadieSettings configuration, string artistSortName, string destinationFolder = null)
        {
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(artistSortName), "Invalid Artist Sort Name");

            var artistFolder = artistSortName.ToTitleCase(false);
            destinationFolder = destinationFolder ?? configuration.LibraryFolder;
            return Path.Combine(destinationFolder, artistFolder.ToFolderNameFriendly());
        }

        /// <summary>
        /// Delete any empty folders in the given folder
        /// </summary>
        /// <param name="processingFolder"></param>
        /// <returns></returns>
        public static bool DeleteEmptyFolders(DirectoryInfo processingFolder)
        {
            if (processingFolder == null || !processingFolder.Exists)
            {
                return true;
            }
            foreach (var folder in processingFolder.GetDirectories("*.*", SearchOption.AllDirectories))
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
            return true;
        }

        /// <summary>
        /// For given artist delete any empty folders
        /// </summary>
        /// <param name="artist">Populated Artist database record</param>
        /// <param name="destinationFolder">Optional Root folder defaults to Library Folder from Settings</param>
        /// <returns></returns>
        public static bool DeleteEmptyFoldersForArtist(IRoadieSettings configuration, Data.Artist artist, string destinationFolder = null)
        {
            destinationFolder = destinationFolder ?? configuration.LibraryFolder;
            SimpleContract.Requires<ArgumentException>(artist != null, "Invalid Artist");
            return FolderPathHelper.DeleteEmptyFolders(new DirectoryInfo(artist.ArtistFileFolder(configuration, destinationFolder)));
        }

        /// <summary>
        /// For a given Track database record determine the path using the given destination folder
        /// </summary>
        /// <param name="track">Populate track database record</param>
        /// <param name="destinationFolder">Optional Root folder defaults to Library Folder from Settings</param>
        public static string PathForTrack(IRoadieSettings configuration, Data.Track track, string destinationFolder = null)
        {
            destinationFolder = destinationFolder ?? configuration.LibraryFolder;
            if (string.IsNullOrEmpty(track.FilePath) || string.IsNullOrEmpty(track.FileName))
            {
                return null;
            }
            return Path.Combine(destinationFolder, track.FilePath, track.FileName);
        }

        /// <summary>
        /// Full path to Release folder using given full Artist folder
        /// </summary>
        /// <param name="artistFolder">Full path to Artist folder</param>
        /// <param name="releaseTitle">Title of Release</param>
        /// <param name="releaseDate">Date of Release</param>
        public static string ReleasePath(string artistFolder, string releaseTitle, DateTime releaseDate)
        {
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(artistFolder), "Invalid Artist Folder");
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(releaseTitle), "Invalid Release Title");
            SimpleContract.Requires<ArgumentException>(releaseDate != DateTime.MinValue, "Invalid Release Date");

            return Path.Combine(artistFolder, string.Format("{1}{0}", releaseTitle.ToTitleCase(false).ToFolderNameFriendly(), string.Format("[{0}] ", releaseDate.ToString("yyyy"))));
        }

        /// <summary>
        /// Returns the FileName for given Track details, this is not the Full Path (FQDN) only the FileName
        /// </summary>
        /// <param name="metaData">Populated Track MetaData</param>
        public static string TrackFileName(IRoadieSettings configuration, AudioMetaData metaData)
        {
            var fileInfo = new FileInfo(metaData.Filename);
            return FolderPathHelper.TrackFileName(configuration, metaData.Release, metaData.TrackNumber ?? 0, metaData.Disk, metaData.TotalTrackNumbers, fileInfo.Extension.ToLower());
        }

        /// <summary>
        /// Returns the FileName for given Track details, this is not the Full Path (FQDN) only the FileName
        /// </summary>
        /// <param name="trackTitle">Title of the Track</param>
        /// <param name="trackNumber">Track Number</param>
        /// <param name="diskNumber">Optional disk number defaults to 0</param>
        /// <param name="totalTrackNumber">Optional Total Tracks defaults to TrackNumber</param>
        /// <param name="fileExtension">Optional File Extension defaults to mp3</param>
        public static string TrackFileName(IRoadieSettings configuration, string trackTitle, short trackNumber, int? diskNumber = null, int? totalTrackNumber = null, string fileExtension = "mp3")
        {
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(trackTitle), "Invalid Track Title");
            SimpleContract.Requires<ArgumentException>(trackNumber > 0, "Invalid Track Number");
            SimpleContract.Requires<ArgumentException>(!string.IsNullOrEmpty(fileExtension), "Invalid File Extension");

            // If the total number of tracks is more than 99 or the track number itself is more than 99 then 3 pad else 2 pad
            var track = (totalTrackNumber ?? trackNumber) > 99 || trackNumber > 99 ? trackNumber.ToString("D3") : trackNumber.ToString("D2");
            // Put an "m" for media on the TPOS greater than 1 so the directory sorts proper
            var dn = diskNumber ?? 0;
            var disk = dn > 1 ? string.Format("m{0} ", dn.ToString("D3")) : string.Empty;

            // Get new name for file
            var fileNameFromTitle = trackTitle.ToTitleCase(false).ToFileNameFriendly();
            if (fileNameFromTitle.StartsWith(track))
            {
                fileNameFromTitle = fileNameFromTitle
                            .RemoveStartsWith(string.Format("{0} -", track))
                            .RemoveStartsWith(string.Format("{0} ", track))
                            .RemoveStartsWith(string.Format("{0}.", track))
                            .RemoveStartsWith(string.Format("{0}-", track))
                            .ToTitleCase(false);
            }
            var trackPathReplace = configuration.TrackPathReplace;
            if (trackPathReplace != null)
            {
                foreach (var kp in trackPathReplace)
                {
                    fileNameFromTitle = fileNameFromTitle.Replace(kp.Key, kp.Value);
                }
            }

            return string.Format("{0}{1} {2}.{3}", disk, track, fileNameFromTitle, fileExtension.ToLower());
        }

        /// <summary>
        /// Returns the Full path (FQDN) for given Track details
        /// </summary>
        /// <param name="metaData">Populated Track MetaData</param>
        /// <param name="destinationFolder">Optional Root folder defaults to Library Folder from Settings</param>
        /// <param name="artistFolder">Optional ArtistFolder default is to get from MetaData artist</param>
        public static string TrackFullPath(IRoadieSettings configuration, AudioMetaData metaData, string destinationFolder = null, string artistFolder = null)
        {
            return FolderPathHelper.TrackFullPath(configuration, metaData.Artist, metaData.Release, SafeParser.ToDateTime(metaData.Year).Value, metaData.Title, metaData.TrackNumber ?? 0, destinationFolder, metaData.Disk ?? 0, metaData.TotalTrackNumbers ?? 0, artistFolder: artistFolder);
        }

        /// <summary>
        /// Returns the Full path (FQDN) for given Track details
        /// </summary>
        /// <param name="artist">Artist For release</param>
        /// <param name="release">Release</param>
        /// <param name="track">Track</param>
        /// <param name="destinationFolder">Optional Root folder defaults to Library Folder from Settings</param>
        /// <returns></returns>
        public static string TrackFullPath(IRoadieSettings configuration, Data.Artist artist, Data.Release release, Data.Track track, string destinationFolder = null)
        {
            return FolderPathHelper.TrackFullPath(configuration, artist.SortNameValue, release.Title, release.ReleaseDate.Value, track.Title, track.TrackNumber, destinationFolder);
        }

        /// <summary>
        /// Return the full path (FQDN) for given Track details
        /// </summary>
        /// <param name="artistSortName">Sort name of Artist to use for folder name</param>
        /// <param name="releaseTitle">Title of Release</param>
        /// <param name="releaseDate">Date of Release</param>
        /// <param name="trackNumber">Track Number</param>
        /// <param name="destinationFolder">Optional Root folder defaults to Library Folder from Settings</param>
        /// <param name="diskNumber">Optional disk number defaults to 0</param>
        /// <param name="totalTrackNumber">Optional Total Tracks defaults to TrackNumber</param>
        /// <param name="fileExtension">Optional File Extension defaults to mp3</param>
        public static string TrackFullPath(IRoadieSettings configuration, string artistSortName, string releaseTitle, DateTime releaseDate, string trackTitle, short trackNumber, string destinationFolder = null, int? diskNumber = null, int? totalTrackNumber = null, string fileExtension = "mp3", string artistFolder = null)
        {
            destinationFolder = destinationFolder ?? configuration.LibraryFolder;
            artistFolder = artistFolder ?? FolderPathHelper.ArtistPath(configuration, artistSortName, destinationFolder);
            var releaseFolder = FolderPathHelper.ReleasePath(artistFolder, releaseTitle, releaseDate);
            var trackFileName = FolderPathHelper.TrackFileName(configuration, trackTitle, trackNumber, diskNumber, totalTrackNumber, fileExtension);

            var result = Path.Combine(artistFolder, releaseFolder, trackFileName);
            Trace.WriteLine(string.Format("TrackPath [{0}] For ArtistName [{1}], ReleaseTitle [{2}], ReleaseDate [{3}], TrackNumber [{4}]", result, artistSortName, releaseTitle, releaseDate, trackNumber));
            return result;
        }

        /// <summary>
        /// Returns the Directory for a Track (just directory not Track FileName)
        /// </summary>
        /// <param name="metaData">Populated Track MetaData</param>
        /// <param name="destinationFolder">Optional Root folder defaults to Library Folder from Settings</param>
        /// <param name="artistFolder">Optional ArtistFolder default is to get from MetaData artist</param>///
        public static string TrackPath(IRoadieSettings configuration, AudioMetaData metaData, string destinationFolder = null, string artistFolder = null)
        {
            var fileInfo = new FileInfo(FolderPathHelper.TrackFullPath(configuration, metaData, destinationFolder, artistFolder));
            return fileInfo.Directory.Name;
        }

        /// <summary>
        /// Returns the Directory for a Track (just directory not Track FileName)
        /// </summary>
        /// <param name="artist">Artist For release</param>
        /// <param name="release">Release</param>
        /// <param name="track">Track</param>
        /// <param name="destinationFolder">Optional Root folder defaults to Library Folder from Settings</param>
        public static string TrackPath(IRoadieSettings configuration, Data.Artist artist, Data.Release release, Data.Track track, string destinationFolder = null)
        {
            var fileInfo = new FileInfo(FolderPathHelper.TrackFullPath(configuration, artist.SortNameValue, release.Title, release.ReleaseDate.Value, track.Title, track.TrackNumber, destinationFolder));
            return fileInfo.Directory.Name;
        }

        /// <summary>
        /// Returns the Directory for a Track (just directory not Track FileName)
        /// </summary>
        /// <param name="artistSortName">Sort name of Artist to use for folder name</param>
        /// <param name="releaseTitle">Title of Release</param>
        /// <param name="releaseDate">Date of Release</param>
        /// <param name="trackNumber">Track Number</param>
        /// <param name="destinationFolder">Optional Root folder defaults to Library Folder from Settings</param>
        /// <param name="diskNumber">Optional disk number defaults to 0</param>
        /// <param name="totalTrackNumber">Optional Total Tracks defaults to TrackNumber</param>
        /// <param name="fileExtension">Optional File Extension defaults to mp3</param>
        public static string TrackPath(IRoadieSettings configuration, string artistSortName, string releaseTitle, DateTime releaseDate, string trackTitle, short trackNumber, string destinationFolder = null, int? diskNumber = null, int? totalTrackNumber = null, string fileExtension = "mp3")
        {
            var fileInfo = new FileInfo(FolderPathHelper.TrackFullPath(configuration, artistSortName, releaseTitle, releaseDate, trackTitle, trackNumber, destinationFolder, diskNumber, totalTrackNumber));
            return fileInfo.Directory.Name;
        }
    }
}