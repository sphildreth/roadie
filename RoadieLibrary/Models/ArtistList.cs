using Roadie.Library.Models.Users;
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
        public UserArtist UserRating { get; set; }
        public int? ReleaseCount { get; set; }
        public int? TrackCount { get; set; }
        public int? PlayedCount { get; set; }
        public Image Thumbnail { get; set; }
        public DateTime? LastPlayed { get; set; }
    }
}
