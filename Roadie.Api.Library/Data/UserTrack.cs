using Roadie.Library.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("usertrack")]
    public class UserTrack : EntityBase
    {
        [Column("isDisliked")] public bool? IsDisliked { get; set; }

        [Column("isFavorite")] public bool? IsFavorite { get; set; }

        [Column("lastPlayed")] public DateTime? LastPlayed { get; set; }

        [Column("playedCount")] public int? PlayedCount { get; set; }

        [Column("rating")] public short Rating { get; set; }

        public Track Track { get; set; }

        [Column("trackId")] [Required] public int TrackId { get; set; }

        public ApplicationUser User { get; set; }

        [Column("userId")] [Required] public int UserId { get; set; }

        public UserTrack()
        {
        }

        public UserTrack(DateTime? now = null)
        {
            PlayedCount = 0;
            Rating = 0;
            IsDisliked = false;
            IsFavorite = false;
            LastPlayed = now ?? DateTime.UtcNow;
        }
    }
}