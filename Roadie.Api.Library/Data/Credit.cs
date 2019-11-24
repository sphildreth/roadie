using Roadie.Library.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("credit")]
    public partial class Credit : NamedEntityBase
    {
        [Column("creditCategoryId")]
        public int CreditCategoryId { get; set; }

        [Column("description")]
        [MaxLength(4000)]
        public string Description { get; set; }

        /// <summary>
        /// Full Name when not an Artist via ArtistId (like a Producer)
        /// </summary>
        [Column("creditToName")]
        [MaxLength(500)]
        public string CreditToName { get; set; }

        [Column("artistId")]
        public int? ArtistId { get; set; }

        [Column("releaseId")]
        public int? ReleaseId { get; set; }

        [Column("trackId")]
        public int? TrackId { get; set; }

        [NotMapped]
        public override string Name { get; set; }

        [NotMapped]
        public override string SortName { get; set; }

        [NotMapped]
        public override string AlternateNames { get; set; }
    }
}
