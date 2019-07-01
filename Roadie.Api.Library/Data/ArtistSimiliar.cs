using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Roadie.Library.Data
{
    [Table("artistSimilar")]
    public partial class ArtistSimilar
    {
        //[ForeignKey("artistId")]
        public Artist Artist { get; set; }

        [Column("artistId")]
        [Required]
        public int ArtistId { get; set; }

        //[ForeignKey("associatedArtistId")]
        public Artist SimilarArtist { get; set; }

        [Column("similarArtistId")]
        public int SimilarArtistId { get; set; }

        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
    }
}