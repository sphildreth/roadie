using Roadie.Library.Configuration;
using Roadie.Library.Enums;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Roadie.Library.Data
{
    public partial class Track
    {
        public string CacheRegion
        {
            get
            {
                return string.Format("urn:track:{0}", this.RoadieId);
            }
        }

        public string Etag
        {
            get
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    return String.Concat(md5.ComputeHash(System.Text.Encoding.Default.GetBytes(string.Format("{0}{1}", this.RoadieId, this.LastUpdated))).Select(x => x.ToString("D2")));
                }
            }
        }

        public bool IsValid
        {
            get
            {
                return !string.IsNullOrEmpty(this.Hash);
            }
        }

        [NotMapped]
        public Artist TrackArtist { get; set; }

        [NotMapped]
        public IEnumerable<string> TrackArtists { get; set; }

        /// <summary>
        /// Returns a full file path to the current track
        /// </summary>
        public string PathToTrack(IRoadieSettings configuration, string libraryFolder)
        {
            return FolderPathHelper.PathForTrack(configuration, this, libraryFolder);
        }

        public override string ToString()
        {
            return string.Format("Id [{0}], TrackNumber [{1}], Title [{2}]", this.Id, this.TrackNumber, this.Title);
        }

        /// <summary>
        /// Update any file related columns to indicate this track file is missing
        /// </summary>
        /// <param name="now">Optional datetime to mark for Updated, if missing defaults to UtcNow</param>
        public void UpdateTrackMissingFile(DateTime? now = null)
        {
            this.Hash = null;
            this.Status = Statuses.Missing;
            this.FileName = null;
            this.FileSize = null;
            this.FilePath = null;
            this.LastUpdated = now ?? DateTime.UtcNow;
        }
    }
}