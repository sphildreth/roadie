using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models.Playlists
{
    [Serializable]
    public class PlaylistList : EntityInfoModelBase
    {
        public DataToken Playlist { get; set; }
        public DataToken User { get; set; }
        public string ThumbnailUrl { get; set; }
        public int? PlaylistCount { get; set; }
        public string UserThumbnailUrl { get; set; }
    }
}
