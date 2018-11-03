using Microsoft.Extensions.Configuration;
using Roadie.Library.Enums;
using Roadie.Library.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace Roadie.Library.Data
{
    public partial class Track
    {
        [NotMapped]
        public IEnumerable<string> TrackArtists { get; set; }

        [NotMapped]
        public Artist TrackArtist { get; set; }

        public bool IsValid
        {
            get
            {
                return !string.IsNullOrEmpty(this.Hash);
            }
        }

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

        /// <summary>
        /// Returns a full file path to the current track
        /// </summary>
        public string PathToTrack(IConfiguration configuration, string libraryFolder)
        {
            return FolderPathHelper.PathForTrack(configuration, this, libraryFolder);
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

        public override string ToString()
        {
            return string.Format("Id [{0}], TrackNumber [{1}], Title [{2}]", this.Id, this.TrackNumber, this.Title);
        }

    }
}
