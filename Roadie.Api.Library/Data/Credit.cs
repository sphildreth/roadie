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

        [ForeignKey(nameof(ArtistId))]
        [InverseProperty("Credits")]
        public virtual Artist Artist { get; set; }

        [ForeignKey(nameof(CreditCategoryId))]
        [InverseProperty("Credits")]
        public virtual CreditCategory CreditCategory { get; set; }

        [ForeignKey(nameof(ReleaseId))]
        [InverseProperty("Credits")]
        public virtual Release Release { get; set; }

        [ForeignKey(nameof(TrackId))]
        [InverseProperty("Credits")]
        public virtual Track Track { get; set; }
    }
}