using Roadie.Library.Configuration;
using Roadie.Library.Enums;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography;

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
                using (var md5 = MD5.Create())
                {
                    return string.Concat(md5
                        .ComputeHash(
                            System.Text.Encoding.Default.GetBytes(string.Format("{0}{1}", RoadieId, LastUpdated)))
                        .Select(x => x.ToString("D2")));
                }
            }
        }

        public bool IsValid => !string.IsNullOrEmpty(Hash);

        public Artist TrackArtist { get; set; }

        [NotMapped] public IEnumerable<string> TrackArtists { get; set; }

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

        /// <summary>
        ///     Returns a full file path to the current track thumbnail (if any)
        /// </summary>
        public string PathToTrackThumbnail(IRoadieSettings configuration)
        {
            return FolderPathHelper.PathForTrackThumbnail(configuration, this);
        }

        public override string ToString()
        {
            return string.Format("Id [{0}], TrackNumber [{1}], Title [{2}]", Id, TrackNumber, Title);
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