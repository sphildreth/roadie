using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.Playlists
{
    [Serializable]
    public class PlaylistTrack
    {
        public TrackList Track { get; set; }
        public int ListNumber { get; set; }
        public int? OldListNumber { get; set; }
    }
}
