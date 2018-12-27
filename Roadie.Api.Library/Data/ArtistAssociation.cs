using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("artistAssociation")]
    public partial class ArtistAssociation
    {
        //[ForeignKey("artistId")]
        public Artist Artist { get; set; }

        [Column("artistId")]
        [Required]
        public int ArtistId { get; set; }

        //[ForeignKey("associatedArtistId")]
        public Artist AssociatedArtist { get; set; }

        [Column("associatedArtistId")]
        public int AssociatedArtistId { get; set; }

        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
    }
}