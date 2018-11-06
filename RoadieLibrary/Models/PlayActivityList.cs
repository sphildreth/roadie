using Newtonsoft.Json;
using Roadie.Library.Models.Users;
using System;

namespace Roadie.Library.Models
{
    [Serializable]
    public class PlayActivityList
    {
        public DataToken Artist { get; set; }
        public string ArtistThumbnailUrl { get; set; }

        public string PlayedDate
        {
            get
            {
                return this.PlayedDateDateTime.HasValue ? this.PlayedDateDateTime.Value.ToString("s") : null;
            }
        }

        [JsonIgnore]
        public DateTime? PlayedDateDateTime { get; set; }

        public int? Rating { get; set; }
        public DataToken Release { get; set; }
        public string ReleasePlayUrl { get; set; }
        public string ReleaseThumbnailUrl { get; set; }
        public DataToken Track { get; set; }
        public DataToken TrackArtist { get; set; }
        public string TrackPlayUrl { get; set; }
        public DataToken User { get; set; }
        public int? UserRating { get; set; }
        public string UserThumbnailUrl { get; set; }
        public UserTrack UserTrack { get; set; }
    }
}