using Newtonsoft.Json;
using Roadie.Library.Models.Users;
using System;

namespace Roadie.Library.Models
{
    [Serializable]
    public class PlayActivityList
    {
        public DataToken Artist { get; set; }
        public Image ArtistThumbnail { get; set; }

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
        public Image ReleaseThumbnail { get; set; }
        public DataToken Track { get; set; }
        public DataToken TrackArtist { get; set; }
        public string TrackPlayUrl { get; set; }
        public DataToken User { get; set; }
        public int? UserRating { get; set; }
        public Image UserThumbnail { get; set; }
        public UserTrack UserTrack { get; set; }

        public bool IsNowPlaying { get; set; }

        public override string ToString()
        {
            return $"User [{ this.User }], Artist [{ this.Artist }], Release [{ this.Release }], Track [{ this.Track}]";
        }
    }
}