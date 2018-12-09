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
        public int? ForArtistId { get; set; }
        public int? ForReleaseId { get; set; }
        public int? NewArtists { get; set; }
        public int? NewReleases { get; set; }
        public int? NewTracks { get; set; }
        public int TimeSpanInSeconds { get; set; }
        [NotMapped]
        public new bool? IsLocked { get; set; }
    }
}
