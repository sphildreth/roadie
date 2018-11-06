using Roadie.Library.Models.Users;
using System;

namespace Roadie.Library.Models
{
    [Serializable]
    public class PlayActivity
    {
        public Guid ArtistId { get; set; }
        public string ArtistName { get; set; }
        public string ArtistThumbnailUrl { get; set; }
        public Guid ReleaseId { get; set; }
        public string ReleaseThumbnailUrl { get; set; }
        public string ReleaseTitle { get; set; }
        public Guid TrackId { get; set; }
        public string TrackTitle { get; set; }
        public User User { get; set; }
        public UserTrack UserTrack { get; set; }
    }
}