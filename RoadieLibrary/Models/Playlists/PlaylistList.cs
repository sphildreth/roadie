using Roadie.Library.Utility;
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
        public Image Thumbnail { get; set; }
        public int? PlaylistCount { get; set; }
        public Image UserThumbnail { get; set; }
        public bool IsPublic { get; set; }
        public decimal? Duration { get; set; }
        public string DurationTime
        {
            get
            {
                if (!this.Duration.HasValue)
                {
                    return "--:--";
                }
                return new TimeInfo(this.Duration.Value).ToFullFormattedString();
            }

        }

        public short TrackCount { get; set; }
    }
}
