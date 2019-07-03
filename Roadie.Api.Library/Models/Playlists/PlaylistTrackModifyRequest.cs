using System;
using System.Collections.Generic;

namespace Roadie.Library.Models.Playlists
{
    [Serializable]
    public class PlaylistTrackModifyRequest
    {
        public Guid Id { get; set; }
        public List<PlaylistTrack> Tracks { get; set; }
    }
}