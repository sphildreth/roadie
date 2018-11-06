using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models
{
    [Serializable]
    public class ArtistList : EntityInfoModelBase
    {
        public DataToken Artist { get; set; }
        public short? Rating { get; set; }
        public short? UserArtistRating { get; set; }
        public int? ArtistReleaseCount { get; set; }
        public int? ArtistTrackCount { get; set; }
        public int? ArtistPlayedCount { get; set; }
        public string ThumbnailUrl { get; set; }
        public int? ThumbnailSize { get; set; }
    }
}
