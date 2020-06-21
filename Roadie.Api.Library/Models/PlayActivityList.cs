using Roadie.Library.Models.Users;
using System;
using System.Text.Json.Serialization;

namespace Roadie.Library.Models
{
    [Serializable]
    public class PlayActivityList
    {

        public DataToken Artist { get; set; }

        public Image ArtistThumbnail { get; set; }

        public bool IsNowPlaying { get; set; }

        public string PlayedDate => PlayedDateDateTime.HasValue ? PlayedDateDateTime.Value.ToString("s") : null;

        [JsonIgnore]
        public DateTime? PlayedDateDateTime { get; set; }

        public string PlayedDay => PlayedDateDateTime.HasValue ? PlayedDateDateTime.Value.ToString("MM/dd/yyyy") : null;

        public int? Rating { get; set; }

        public DataToken Release { get; set; }

        public string ReleasePlayUrl { get; set; }

        public Image ReleaseThumbnail { get; set; }

        public TrackList Track { get; set; }

        public DataToken TrackArtist { get; set; }

        public string TrackPlayUrl { get; set; }

        public DataToken User { get; set; }

        public int? UserRating { get; set; }

        public Image UserThumbnail { get; set; }

        public UserTrack UserTrack { get; set; }

        public override string ToString()
        {
            return $"User [{User}], Artist [{Artist}], Release [{Release}], Track [{Track}]";
        }

    }
}