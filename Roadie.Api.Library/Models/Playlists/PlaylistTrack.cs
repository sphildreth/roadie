using System;

namespace Roadie.Library.Models.Playlists
{
    [Serializable]
    public class PlaylistTrack
    {
        public int ListNumber { get; set; }
        public int? OldListNumber { get; set; }
        public TrackList Track { get; set; }
    }
}