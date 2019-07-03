using System;

namespace Roadie.Library.Models.Playlists
{
    [Serializable]
    public class PlaylistTrackList : EntityInfoModelBase
    {
        public DataToken Artist { get; set; }
        public string ArtistThumbnailUrl { get; set; }
        public int ListNumber { get; set; }
        public DataToken Release { get; set; }
        public string ReleaseThumbnailUrl { get; set; }
        public DataToken Track { get; set; }
        public string TrackThumbnailUrl { get; set; }
    }
}