using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("artistAssociation")]
    public partial class ArtistAssociation
    {
        public Artist Artist { get; set; }

        [Column("artistId")]
        [Required]
        public int ArtistId { get; set; }

        public Artist AssociatedArtist { get; set; }

        [Column("associatedArtistId")]
        [Required]
        public int AssociatedArtistId { get; set; }

        [Key]
        [Column("id")]
        public int Id { get; set; }
    }
}