using System;

namespace Roadie.Library.Models.Users
{
    public class UserTrack : UserRatingBase
    {
        public DateTime? LastPlayed { get; set; }
        public int? PlayedCount { get; set; }
        public DataToken Track { get; set; }
        public DataToken User { get; set; }
    }
}