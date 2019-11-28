using Roadie.Library.Enums;
using Roadie.Library.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("scanHistory")]
    public class ScanHistory : EntityBase
    {
        [Column("forArtistId")]
        public int? ForArtistId { get; set; }

        [Column("forReleaseId")]
        public int? ForReleaseId { get; set; }

        [NotMapped]
        public new bool? IsLocked { get; set; }

        [Column("newArtists")]
        public int? NewArtists { get; set; }

        [Column("newReleases")]
        public int? NewReleases { get; set; }

        [Column("newTracks")]
        public int? NewTracks { get; set; }

        [Column("timeSpanInSeconds")]
        public int TimeSpanInSeconds { get; set; }

        public virtual User User { get; set; }

        [Column("userId")]
        public int UserId { get; set; }

        public ScanHistory()
        {
            Status = Statuses.Complete;
        }
    }
}