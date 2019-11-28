using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("artistAssociation")]
    public class ArtistAssociation
    {
        [ForeignKey(nameof(ArtistId))]
        [InverseProperty("AssociatedArtists")]
        public virtual Artist Artist { get; set; }

        [Column("artistId")]
        [Required]
        public int ArtistId { get; set; }

        [ForeignKey(nameof(AssociatedArtistId))]
        public virtual Artist AssociatedArtist { get; set; }

        [Column("associatedArtistId")]
        [Required]
        public int AssociatedArtistId { get; set; }

        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
    }
}