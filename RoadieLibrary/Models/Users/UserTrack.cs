using System;

namespace Roadie.Library.Models.Users
{
    public class UserTrack : UserRatingBase
    {
        public DateTime? LastPlayed { get; set; }
        public int? PlayedCount { get; set; }
        public Guid TrackId { get; set; }
        public Guid UserId { get; set; }
    }
}