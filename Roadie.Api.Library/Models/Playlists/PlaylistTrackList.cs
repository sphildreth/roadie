using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.Playlists
{
    [Serializable]
    public class PlaylistTrackList : EntityInfoModelBase
    {
        public DataToken Track { get; set; }
        public DataToken Release { get; set; }
        public DataToken Artist { get; set; }
        public int ListNumber { get; set; }
        public string TrackThumbnailUrl { get; set; }
        public string ReleaseThumbnailUrl { get; set; }
        public string ArtistThumbnailUrl { get; set; }
    }
}
