using Roadie.Library.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Roadie.Library.Data
{
    [Table("scanHistory")]
    public class ScanHistory : EntityBase
    {
        [Column("userId")]
        public int UserId { get; set; }
        public ApplicationUser User { get; set; }
        [Column("forArtistId")]
        public int? ForArtistId { get; set; }
        [Column("forReleaseId")]
        public int? ForReleaseId { get; set; }
        [Column("newArtists")]
        public int? NewArtists { get; set; }
        [Column("newReleases")]
        public int? NewReleases { get; set; }
        [Column("newTracks")]
        public int? NewTracks { get; set; }
        [Column("timeSpanInSeconds")]
        public int TimeSpanInSeconds { get; set; }
        [NotMapped]
        public new bool? IsLocked { get; set; }

        public ScanHistory()
        {
            this.Status = Enums.Statuses.Complete;
        }
    }
}
