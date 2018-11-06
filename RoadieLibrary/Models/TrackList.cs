using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Models
{
    [Serializable]
    public class TrackList : EntityInfoModelBase
    {
        public int? TrackNumber { get; set; }
        public int? ReleaseMediaNumber { get; set; }
        public Guid ReleaseId { get; set; }
        public string ReleaseThumbnailUrl { get; set; }
        public string ReleaseTitle { get; set; }
        public Guid ReleaseArtistId { get; set; }
        public string ReleaseArtistName { get; set; }
        public string ReleaseArtistThumbnail { get; set; }
        public DataToken TrackArtist { get; set; }
        public string TrackArtistThumbnail { get; set; }
        public string Title { get; set; }
        public int? Duration { get; set; }
        public string DurationTime
        {
            get
            {
                return this.Duration.HasValue ? TimeSpan.FromSeconds(this.Duration.Value / 1000).ToString(@"hh\:mm\:ss") : "--:--";
            }
        }
        public string DurationTimeShort
        {
            get
            {
                return this.Duration.HasValue ? TimeSpan.FromSeconds(this.Duration.Value / 1000).ToString(@"mm\:ss") : "--:--";
            }
        }
        public short? Rating { get; set; }
        public short? UserRating { get; set; }
        public bool? IsUserFavorite { get; set; }
        public bool? IsUserDisliked { get; set; }
        public int? PlayedCount { get; set; }
        public int? FavoriteCount { get; set; }
        public string ThumbnailUrl { get; set; }
        public string TrackPlayUrl { get; set; }
    }
}
