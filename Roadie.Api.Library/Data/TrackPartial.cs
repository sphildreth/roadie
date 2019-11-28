using Roadie.Library.Configuration;
using Roadie.Library.Enums;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace Roadie.Library.Data
{
    public partial class Track
    {
        public string CacheKey => CacheUrn(RoadieId);

        public string CacheRegion => CacheRegionUrn(RoadieId);

        public string Etag
        {
            get
            {
                return HashHelper.CreateMD5($"{ RoadieId}{ LastUpdated}");
            }
        }

        /// <summary>
        /// Are the track details valid so the track can play.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return !string.IsNullOrEmpty(Hash) &&
                       !string.IsNullOrEmpty(FileName) &&
                       FileSize.HasValue &&
                       !string.IsNullOrEmpty(FilePath);
            }
        }

        /// <summary>
        /// This is used when finding metadata, not storedin the database.
        /// </summary>
        [NotMapped]
        public virtual IEnumerable<string> TrackArtists { get; set; }

        public static string CacheRegionUrn(Guid Id)
        {
            return string.Format("urn:track:{0}", Id);
        }

        public static string CacheUrn(Guid Id)
        {
            return $"urn:track_by_id:{Id}";
        }

        /// <summary>
        ///     Returns a full file path to the current track
        /// </summary>
        public string PathToTrack(IRoadieSettings configuration)
        {
            return FolderPathHelper.PathForTrack(configuration, this);
        }

        public bool DoesFileForTrackExist(IRoadieSettings configuration)
        {
            var trackPath = PathToTrack(configuration);
            if (string.IsNullOrEmpty(trackPath))
            {
                return false;
            }
            return File.Exists(trackPath);
        }

        /// <summary>
        ///     Returns a full file path to the current track thumbnail (if any)
        /// </summary>
        public string PathToTrackThumbnail(IRoadieSettings configuration)
        {
            return FolderPathHelper.PathForTrackThumbnail(configuration, this);
        }

        public override string ToString()
        {
            return $"Id [{ Id }], Status [{ Status }], TrackNumber [{ TrackNumber }], Title [{ Title}]";
        }

        /// <summary>
        ///     Update any file related columns to indicate this track file is missing
        /// </summary>
        /// <param name="now">Optional datetime to mark for Updated, if missing defaults to UtcNow</param>
        public void UpdateTrackMissingFile(DateTime? now = null)
        {
            Hash = null;
            Status = Statuses.Missing;
            FileName = null;
            FileSize = null;
            FilePath = null;
            LastUpdated = now ?? DateTime.UtcNow;
        }
    }
}